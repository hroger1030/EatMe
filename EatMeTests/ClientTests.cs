using EatMeLibrary;

namespace EatMeTests
{
    public class ClientTests
    {
        [TestCase("192.168.1.5", "192.168.1.5", NetworkConstants.DEFAULT_PORT)]
        [TestCase("192.168.1.5:9000", "192.168.1.5", 9000)]
        [TestCase("  game.example.com:7777  ", "game.example.com", 7777)]
        [TestCase("localhost", "localhost", NetworkConstants.DEFAULT_PORT)]
        [TestCase("::1", "::1", NetworkConstants.DEFAULT_PORT)]
        public void ServerAddress_TryParse_Valid(string text, string expectedHost, int expectedPort)
        {
            Assert.That(ServerAddress.TryParse(text, out var host, out var port), Is.True);
            Assert.That(host, Is.EqualTo(expectedHost));
            Assert.That(port, Is.EqualTo(expectedPort));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        [TestCase("host:")]
        [TestCase("host:abc")]
        [TestCase("host:0")]
        [TestCase("host:70000")]
        [TestCase(":7777")]
        public void ServerAddress_TryParse_Invalid(string text)
        {
            Assert.That(ServerAddress.TryParse(text, out _, out _), Is.False);
        }

        private static ClientGameState JoinedState(int playerId = 3)
        {
            var state = new ClientGameState();
            state.Apply(new WelcomeMessage { PlayerId = playerId, Team = eTeam.Blue, ServerName = "Srv", MessageOfTheDay = "Play nice", LoadingScreenTips = ["Hi"] }, 0);
            state.Apply(new LevelStartMessage { Level = 2, WallColor = FastColor.FromRgb(1, 2, 3), WallFillColor = FastColor.FromRgb(7, 8, 9), BackgroundColor = FastColor.FromRgb(4, 5, 6) }, 0);
            return state;
        }

        [Test]
        public void ClientState_LevelWon_RanksEachTeamAndCountsDown()
        {
            var state = JoinedState();

            state.Apply(new LevelWonMessage
            {
                WinningTeam = eTeam.Blue,
                RedScore = 40,
                BlueScore = 100,
                MsRemaining = 10000,
                Results =
                [
                    new PlayerResult(1, "Zed", eTeam.Blue, 5),
                    new PlayerResult(2, "Amy", eTeam.Red, 9),
                    new PlayerResult(3, "bob", eTeam.Blue, 20),
                    new PlayerResult(4, "Al", eTeam.Blue, 5),
                ],
            }, 1000);

            Assert.That(state.IsIntermission, Is.True);
            Assert.That(state.IntermissionTip, Is.EqualTo("Hi"));
            Assert.That(state.LevelWinner, Is.EqualTo(eTeam.Blue));
            Assert.That(state.LevelBlueScore, Is.EqualTo(100));
            Assert.That(state.LevelRedScore, Is.EqualTo(40));
            Assert.That(state.BlueResults.Select(r => r.Name), Is.EqualTo(new[] { "bob", "Al", "Zed" }));
            Assert.That(state.RedResults.Select(r => r.Name), Is.EqualTo(new[] { "Amy" }));

            Assert.That(state.GetIntermissionSecondsLeft(1000), Is.EqualTo(10));
            Assert.That(state.GetIntermissionSecondsLeft(5800), Is.EqualTo(5));
            Assert.That(state.GetIntermissionSecondsLeft(10800), Is.EqualTo(0));
            Assert.That(state.GetIntermissionSecondsLeft(20000), Is.EqualTo(0));

            state.Apply(new LevelStartMessage { Level = 3 }, 11000);

            Assert.That(state.IsIntermission, Is.False);
            Assert.That(state.BlueResults, Is.Empty);
            Assert.That(state.RedResults, Is.Empty);
        }

        [Test]
        public void ClientState_Welcome_SetsIdentity()
        {
            var state = JoinedState();

            Assert.That(state.IsJoined, Is.True);
            Assert.That(state.PlayerId, Is.EqualTo(3));
            Assert.That(state.Team, Is.EqualTo(eTeam.Blue));
            Assert.That(state.LoadingScreenTips, Is.EqualTo(new[] { "Hi" }));
            Assert.That(state.MessageOfTheDay, Is.EqualTo("Play nice"));
        }

        [Test]
        public void ClientState_LevelStart_BuildsSameMapAsGenerator()
        {
            var state = JoinedState();

            Assert.That(state.Level, Is.EqualTo(2));
            Assert.That(state.LevelInfo, Is.EqualTo(Levels.GetLevel(2)));
            Assert.That(state.WallColor.PackedValue, Is.EqualTo(FastColor.FromRgb(1, 2, 3).PackedValue));
            Assert.That(state.WallFillColor.PackedValue, Is.EqualTo(FastColor.FromRgb(7, 8, 9).PackedValue));
            Assert.That(state.BackgroundColor.PackedValue, Is.EqualTo(FastColor.FromRgb(4, 5, 6).PackedValue));
            Assert.That(state.Map, Is.EqualTo(MapMaker.Generate(new GameConfiguration(), Levels.GetSeed(0, 2))));
        }

        [Test]
        public void ClientState_LevelStart_BuildsMapFromServerSeed()
        {
            var config = new GameConfiguration { ServerSeed = 4242 };
            var state = new ClientGameState();
            state.Apply(new WelcomeMessage { PlayerId = 3, Team = eTeam.Blue, ServerName = "Srv", LoadingScreenTips = [], GameConfig = config }, 0);
            state.Apply(new LevelStartMessage { Level = 2 }, 0);

            Assert.That(state.Map, Is.EqualTo(MapMaker.Generate(config, Levels.GetSeed(4242, 2))));
        }

        [Test]
        public void ClientState_LevelStart_ColorsDotsByHalf()
        {
            var blue = FastColor.FromRgb(200, 225, 255);
            var red = FastColor.FromRgb(255, 205, 205);
            var state = new ClientGameState { BlueDotColor = blue, RedDotColor = red };
            state.Apply(new WelcomeMessage { PlayerId = 3, Team = eTeam.Blue, ServerName = "Srv", LoadingScreenTips = [] }, 0);
            state.Apply(new LevelStartMessage { Level = 1 }, 0);

            int width = state.Map.GetLength(0);
            Assert.That(state.DotColors.GetLength(0), Is.EqualTo(width));
            Assert.That(state.DotColors.GetLength(1), Is.EqualTo(state.Map.GetLength(1)));
            Assert.That(state.DotColors[0, 5], Is.EqualTo(blue));
            Assert.That(state.DotColors[width / 2 - 1, 5], Is.EqualTo(blue));
            Assert.That(state.DotColors[width / 2, 5], Is.EqualTo(red));
            Assert.That(state.DotColors[width - 1, 5], Is.EqualTo(red));
        }

        [Test]
        public void BuildDotColors_LeftHalfBlue_RightHalfRed()
        {
            var blue = FastColor.FromRgb(0, 0, 255);
            var red = FastColor.FromRgb(255, 0, 0);

            var colors = ClientGameState.BuildDotColors(6, 3, blue, red);

            for (int x = 0; x < 6; x++)
                for (int y = 0; y < 3; y++)
                    Assert.That(colors[x, y], Is.EqualTo(x < 3 ? blue : red), $"({x}, {y})");
        }

        [Test]
        public void ClientState_LevelStart_ResetsDotColors()
        {
            var state = JoinedState();
            state.DotColors[1, 1] = FastColor.FromRgb(1, 2, 3);

            state.Apply(new LevelStartMessage { Level = 3 }, 0);

            Assert.That(state.DotColors[1, 1], Is.EqualTo(state.BlueDotColor));
        }

        [Test]
        public void ClientState_OlderSnapshot_Ignored()
        {
            var state = JoinedState();

            state.Apply(new SnapshotMessage { Tick = 10 }, 0);
            state.Apply(new SnapshotMessage { Tick = 5 }, 0);

            Assert.That(state.Snapshot.Tick, Is.EqualTo(10));
        }

        [Test]
        public void ClientState_GetOwnPac_FindsOurPac()
        {
            var state = JoinedState(playerId: 3);

            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(1, eTeam.Red, 1, 1, 50, eFacing.Up, true, false, false), new PacState(3, eTeam.Blue, 7, 8, 50, eFacing.Left, true, false, false)] }, 0);

            Assert.That(state.GetOwnPac()?.X, Is.EqualTo(7));
        }

        [Test]
        public void ClientState_CellsChanged_UpdatesMapAndIgnoresOutOfRange()
        {
            var state = JoinedState();

            state.Apply(new CellsChangedMessage { Cells = [new CellChange(1, 1, eGridDef.PowerPill), new CellChange(60000, 1, eGridDef.Food)] }, 0);

            Assert.That(state.Map[1, 1], Is.EqualTo(eGridDef.PowerPill));
        }

        private static (int X, int Y) FindInnerWall(eGridDef[,] map)
        {
            for (var x = 1; x < map.GetLength(0) - 1; x++)
                for (var y = 1; y < map.GetLength(1) - 1; y++)
                    if (map[x, y].HasFlag(eGridDef.Wall))
                        return (x, y);

            throw new InvalidOperationException("no inner wall");
        }

        [Test]
        public void ClientState_SmashedWall_BreaksIntoFourPieces()
        {
            var state = JoinedState();
            var (x, y) = FindInnerWall(state.Map);

            state.Apply(new CellsChangedMessage { Cells = [new CellChange((ushort)x, (ushort)y, eGridDef.Food)] }, 0);

            Assert.That(state.NewDebris.Count, Is.EqualTo(WallDebris.PIECES_PER_WALL));
            Assert.That(state.NewDebris.All(p => p.CellX == x && p.CellY == y), Is.True);
        }

        [Test]
        public void ClientState_CatchUpCells_MakeNoDebris()
        {
            var state = JoinedState();
            var (x, y) = FindInnerWall(state.Map);

            state.Apply(new CellsChangedMessage { IsCatchUp = true, Cells = [new CellChange((ushort)x, (ushort)y, eGridDef.Food)] }, 0);

            Assert.That(state.Map[x, y], Is.EqualTo(eGridDef.Food));
            Assert.That(state.NewDebris, Is.Empty);
        }

        [Test]
        public void ClientState_LevelStart_ClearsDebris()
        {
            var state = JoinedState();
            var (x, y) = FindInnerWall(state.Map);
            state.Apply(new CellsChangedMessage { Cells = [new CellChange((ushort)x, (ushort)y, eGridDef.Food)] }, 0);

            state.Apply(new LevelStartMessage { Level = 3 }, 0);

            Assert.That(state.NewDebris, Is.Empty);
        }

        [Test]
        public void IsSmashed_OnlyWallsThatGoAndNotTetrisBlocks()
        {
            Assert.That(ClientGameState.IsSmashed(eGridDef.Wall, eGridDef.Food), Is.True);
            Assert.That(ClientGameState.IsSmashed(eGridDef.Wall | eGridDef.TetrisBlock, (eGridDef)0), Is.False);
            Assert.That(ClientGameState.IsSmashed(eGridDef.Food, (eGridDef)0), Is.False);
            Assert.That(ClientGameState.IsSmashed((eGridDef)0, eGridDef.Wall | eGridDef.TetrisBlock), Is.False);
        }

        [Test]
        public void ClientState_PlayersJoinAndLeave()
        {
            var state = JoinedState();

            state.Apply(new PlayerJoinedMessage { PlayerId = 4, Name = "Inky", Team = eTeam.Red }, 0);
            Assert.That(state.GetPlayerName(4), Is.EqualTo("Inky"));

            state.Apply(new PlayerLeftMessage { PlayerId = 4 }, 0);
            Assert.That(state.Players.ContainsKey(4), Is.False);
        }

        [Test]
        public void ClientState_PlayerJoined_KeepsPacShape()
        {
            var state = JoinedState();

            state.Apply(new PlayerJoinedMessage { PlayerId = 4, Name = "Inky", Team = eTeam.Red, PacShape = ePacShape.Boxie }, 0);
            state.Apply(new PlayerJoinedMessage { PlayerId = 5, Name = "Pinky", Team = eTeam.Red, PacShape = (ePacShape)200 }, 0);

            Assert.That(state.GetPacShape(4), Is.EqualTo(ePacShape.Boxie));
            Assert.That(state.GetPacShape(5), Is.EqualTo(PacShapes.DEFAULT));
            Assert.That(state.GetPacShape(99), Is.EqualTo(PacShapes.DEFAULT));
        }

        [Test]
        public void ClientState_PlayerJoinedAgain_UpdatesPacShape()
        {
            var state = JoinedState();

            state.Apply(new PlayerJoinedMessage { PlayerId = 4, Name = "Inky", Team = eTeam.Red, PacShape = ePacShape.Boxie }, 0);
            state.Apply(new PlayerJoinedMessage { PlayerId = 4, Name = "Inky", Team = eTeam.Red, PacShape = ePacShape.Go }, 0);

            Assert.That(state.GetPacShape(4), Is.EqualTo(ePacShape.Go));
        }

        [Test]
        public void ClientState_Pong_SetsPing()
        {
            var state = JoinedState();

            state.Apply(new PongMessage { ClientTime = 1000 }, 1042);

            Assert.That(state.PingMs, Is.EqualTo(42));
        }

        [Test]
        public void ClientState_GetLevelTitle_ShowsNumberAndName()
        {
            var state = JoinedState();

            Assert.That(state.GetLevelTitle(), Is.EqualTo($"Level 2 {Levels.GetLevel(2).Name} ({state.GameConfig.MaxScore}pt to win)"));
        }

        [Test]
        public void ClientState_GetLevelTitle_FollowsAChangedMaxScore()
        {
            var state = JoinedState();
            state.GameConfig.MaxScore = 250;

            Assert.That(state.GetLevelTitle(), Does.EndWith("(250pt to win)"));
        }

        [Test]
        public void ClientState_GetLevelTitle_EmptyBeforeLevelStart()
        {
            Assert.That(new ClientGameState().GetLevelTitle(), Is.Empty);
        }

        [Test]
        public void ClientState_GetPingText_UnknownBeforeFirstPong()
        {
            var state = JoinedState();

            Assert.That(state.GetPingText(), Is.EqualTo("Ping --"));
        }

        [Test]
        public void ClientState_GetPingText_ShowsMs()
        {
            var state = JoinedState();

            state.Apply(new PongMessage { ClientTime = 1000 }, 1042);

            Assert.That(state.GetPingText(), Is.EqualTo("Ping 42 ms"));
        }

        [Test]
        public void ClientState_Reject_Recorded()
        {
            var state = new ClientGameState();

            state.Apply(new RejectMessage { Reason = eRejectReason.ServerFull }, 0);

            Assert.That(state.RejectReason, Is.EqualTo(eRejectReason.ServerFull));
            Assert.That(state.IsJoined, Is.False);
        }

        [Test]
        public void IsOwnPacInEffectRange_EveryoneInsideTheSquare()
        {
            var state = JoinedState(playerId: 3);
            var pow = new GameEventMessage { EventType = eGameEvent.Blast, ActorId = 7, X = 10.5f, Y = 10.5f };

            Assert.That(state.IsOwnPacInEffectRange(pow), Is.False, "no snapshot yet");

            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f + PowerUpEffects.EFFECT_RADIUS, 10.5f + PowerUpEffects.EFFECT_RADIUS, 50f, eFacing.Left, true, false, false)] }, 0);
            Assert.That(state.IsOwnPacInEffectRange(pow), Is.True, "the square's corner");
            Assert.That(state.IsOwnPacInEffectRange(new GameEventMessage { EventType = eGameEvent.Blast, ActorId = 3, X = 10.5f, Y = 10.5f }), Is.True, "the pac that set it off too");

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 10.5f + PowerUpEffects.EFFECT_RADIUS + 1f, 50f, eFacing.Left, true, false, false)] }, 0);
            Assert.That(state.IsOwnPacInEffectRange(pow), Is.False, "out of range");

            state.Apply(new SnapshotMessage { Tick = 3, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 12.5f, 50f, eFacing.Left, false, false, false)] }, 0);
            Assert.That(state.IsOwnPacInEffectRange(pow), Is.True, "dead pacs too, the camera is still there");
        }

        [Test]
        public void ShakesScreen_OnlyBlast()
        {
            foreach (var eventType in Enum.GetValues<eGameEvent>())
                Assert.That(PowerUpEffects.ShakesScreen(eventType), Is.EqualTo(eventType == eGameEvent.Blast), eventType.ToString());
        }

        [Test]
        public void IsOwnPacEating_FollowsOwnPacsEatingFlag()
        {
            var state = JoinedState(playerId: 3);

            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, true)] }, 0);
            Assert.That(state.IsOwnPacEating(), Is.True);

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false), new PacState(4, eTeam.Red, 1.5f, 1.5f, 50f, eFacing.Left, true, false, true)] }, 0);
            Assert.That(state.IsOwnPacEating(), Is.False, "only another pac is eating");

            state.Apply(new SnapshotMessage { Tick = 3, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, false, false, true)] }, 0);
            Assert.That(state.IsOwnPacEating(), Is.False, "a dead pac doesn't chomp");
        }

        [Test]
        public void IsOwnPacNotEating_TrueOnlyWhileOwnPacIsAliveAndNotEating()
        {
            var state = JoinedState(playerId: 3);
            Assert.That(state.IsOwnPacNotEating(), Is.False, "no pac yet");

            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false)] }, 0);
            Assert.That(state.IsOwnPacNotEating(), Is.True);

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, true)] }, 0);
            Assert.That(state.IsOwnPacNotEating(), Is.False, "eating");

            state.Apply(new SnapshotMessage { Tick = 3, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, false, false, false)] }, 0);
            Assert.That(state.IsOwnPacNotEating(), Is.False, "a dead pac has no siren");
        }

        [Test]
        public void IsOwnPacPowered_FollowsOwnPacsPoweredFlag()
        {
            var state = JoinedState(playerId: 3);

            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false, powerUpEffect: ePowerUp.PowerPill)] }, 0);
            Assert.That(state.IsOwnPacPowered(), Is.True);

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false), new PacState(4, eTeam.Red, 1.5f, 1.5f, 50f, eFacing.Left, true, false, false, powerUpEffect: ePowerUp.PowerPill)] }, 0);
            Assert.That(state.IsOwnPacPowered(), Is.False, "only another pac is powered");

            state.Apply(new SnapshotMessage { Tick = 3, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, false, false, false, powerUpEffect: ePowerUp.PowerPill)] }, 0);
            Assert.That(state.IsOwnPacPowered(), Is.False, "a dead pac isn't powered");
        }

        [Test]
        public void OwnPowerUp_FollowsOwnPacsEffectAndTimeLeft()
        {
            var state = JoinedState(playerId: 3);
            Assert.That(state.GetOwnPowerUpEffect(), Is.EqualTo(ePowerUp.None), "no snapshot yet");
            Assert.That(state.GetOwnPowerUpFill(), Is.EqualTo(0f));

            var duration = new GameConfiguration().PowerUpDurationMs;
            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false, powerUpEffect: ePowerUp.PowerPill, powerUpMsLeft: duration / 4)] }, 0);
            Assert.That(state.GetOwnPowerUpEffect(), Is.EqualTo(ePowerUp.PowerPill));
            Assert.That(state.GetOwnPowerUpFill(), Is.EqualTo(0.25f).Within(0.0001f));

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, false, false, false, powerUpEffect: ePowerUp.PowerPill, powerUpMsLeft: duration)] }, 0);
            Assert.That(state.GetOwnPowerUpEffect(), Is.EqualTo(ePowerUp.None), "a dead pac has no effect");
            Assert.That(state.GetOwnPowerUpFill(), Is.EqualTo(0f));
        }

        [Test]
        public void OwnHunger_FollowsOwnPacsTimeSinceEaten()
        {
            var state = JoinedState(playerId: 3);
            Assert.That(state.GetOwnHungerFill(), Is.EqualTo(0f), "no snapshot yet");

            var msUntilStarvation = new GameConfiguration().MsUntilStarvation;
            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false, msSinceEaten: msUntilStarvation / 2)] }, 0);
            Assert.That(state.GetOwnHungerFill(), Is.EqualTo(0.5f).Within(0.0001f));

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, false, false, false, msSinceEaten: msUntilStarvation)] }, 0);
            Assert.That(state.GetOwnHungerFill(), Is.EqualTo(0f), "a dead pac isn't hungry");
        }

        [Test]
        public void OwnStunned_FollowsOwnLivingPac()
        {
            var state = JoinedState(playerId: 3);
            Assert.That(state.IsOwnPacStunned(), Is.False, "no snapshot yet");

            state.Apply(new SnapshotMessage { Tick = 1, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false, stunned: true)] }, 0);
            Assert.That(state.IsOwnPacStunned(), Is.True);

            state.Apply(new SnapshotMessage { Tick = 2, Pacs = [new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false)] }, 0);
            Assert.That(state.IsOwnPacStunned(), Is.False, "reverts when the stun ends");

            state.Apply(new SnapshotMessage { Tick = 3, Pacs = [new PacState(4, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, true, false, false, stunned: true),
                new PacState(3, eTeam.Blue, 10.5f, 20.5f, 50f, eFacing.Left, false, false, false, stunned: true)] }, 0);
            Assert.That(state.IsOwnPacStunned(), Is.False, "another pac's stun and a dead own pac don't count");
        }
    }
}
