using EatMeLibrary;

namespace EatMeTests
{
    public class MessageSerializerTests
    {
        /// <summary>
        /// One message of every type, with every field set to something other than its default.
        /// </summary>
        public static IEnumerable<INetworkMessage> SampleMessages()
        {
            yield return new HelloMessage { ClientVersion = 1.5f, PlayerName = "Blinky42", PreferredTeam = eTeamPreference.Blue };
            yield return new InputMessage { Sequence = 123456, Direction = eDirection.Left };
            yield return new PingMessage { ClientTime = 9876543210 };
            yield return new LeaveMessage();
            yield return new WelcomeMessage { PlayerId = 17, Team = eTeam.Red, ServerName = "Test Server", MessageOfTheDay = "Visit us", LoadingScreenTips = ["Hi there", "Second line"], GameConfig = new GameConfiguration { MazeSize = 64, PacMaxSize = 75.5f } };
            yield return new RejectMessage { Reason = eRejectReason.VersionMismatch };
            yield return new LevelStartMessage { Level = 3, WallColor = FastColor.FromArgb(255, 33, 33, 222) };
            yield return new SnapshotMessage
            {
                Tick = 4000,
                RedScore = 12,
                BlueScore = 40,
                Pacs = [new PacState(1, eTeam.Red, 1.5f, 2.25f, 10f, eFacing.Up, true, false, true), new PacState(2, eTeam.Blue, 98f, 3f, 50.5f, eFacing.Right, false, true, false, 4, 7, 2, 9, ePowerUpEffect.PowerPill, 4321, 1234, 750, true)],
                Ghosts = [new GhostState(0, 50f, 50f, eFacing.Down, true), new GhostState(1, 5f, 6f, eFacing.Left, true, true)],
                Flags = [new FlagState(0, eTeam.Red, 3.5f, 40.5f, FlagState.NO_CARRIER), new FlagState(1, eTeam.Blue, 12f, 13f, 1)],
                PowerUps = [new PowerUpState(0, ePowerUp.Cherry, 49.5f, 50.25f), new PowerUpState(7, ePowerUp.PokeBall, 1f, 2f)],
                WeaponEffects = [new WeaponEffectState(3, eWeaponEffect.Sword, 12.25f, 4.5f, eFacing.Left), new WeaponEffectState(9, eWeaponEffect.HolyFlame, 1.5f, 2.5f, eFacing.Up)],
            };
            yield return new CellsChangedMessage { IsCatchUp = true, Cells = [new CellChange(4, 5, eGridDef.Food), new CellChange(99, 0, eGridDef.PowerPill | eGridDef.SpawnPointRed)] };
            yield return new PlayerJoinedMessage { PlayerId = 5, Name = "Inky", Team = eTeam.Blue };
            yield return new PlayerLeftMessage { PlayerId = 5 };
            yield return new GameEventMessage { EventType = eGameEvent.FlagCaptured, ActorId = 3, TargetId = 8, X = 10.5f, Y = 20.25f, Team = eTeam.Blue };
            yield return new LevelWonMessage
            {
                WinningTeam = eTeam.Blue,
                RedScore = 80,
                BlueScore = 100,
                MsRemaining = 9500,
                Results = [new PlayerResult(1, "Pinky", eTeam.Red, 12), new PlayerResult(1_000_001, "Clyde", eTeam.Blue, 40)],
            };
            yield return new PongMessage { ClientTime = 1234 };
            yield return new ServerPingMessage { ServerTime = 5551234567 };
            yield return new ServerPongMessage { ServerTime = 7654321 };
            yield return new ActionMessage();
            yield return new PlayerListRequestMessage();
            yield return new PlayerListMessage { Entries = [new PlayerListEntry(1, "Pac", eTeam.Red, false, 42), new PlayerListEntry(1000, "Blinky", eTeam.Blue, true, PlayerListEntry.NO_PING)] };
            yield return new GameConfigMessage { GameConfig = new GameConfiguration { MazeSize = 40, PacSpeed = 7.5f, MinPlayers = 6 } };
            yield return new HighScoresMessage { Scores = [new HighScoreEntry { Name = "Smith, John", Score = 1522400, Date = "2024-04-12" }, new HighScoreEntry { Name = "Dios", Score = 200, Date = "2026-01-09" }] };
            yield return new DiscoveryRequestMessage();
            yield return new DiscoveryResponseMessage { ServerVersion = 1.5f, ServerName = "LAN Party", Port = 7777, PlayerCount = 12, MaxPlayers = 128 };
        }

        private static byte[] GetBody(byte[] frame)
        {
            return frame[NetworkConstants.LENGTH_PREFIX_SIZE..];
        }

        [TestCaseSource(nameof(SampleMessages))]
        public void RoundTrip_ProducesIdenticalBytes(INetworkMessage message)
        {
            var frame = MessageSerializer.Serialize(message);
            var copy = MessageSerializer.Deserialize(GetBody(frame));

            Assert.That(copy.MessageType, Is.EqualTo(message.MessageType));
            Assert.That(MessageSerializer.Serialize(copy), Is.EqualTo(frame));
        }

        [TestCaseSource(nameof(SampleMessages))]
        public void Serialize_WritesBodyLengthPrefix(INetworkMessage message)
        {
            var frame = MessageSerializer.Serialize(message);

            Assert.That(BitConverter.ToInt32(frame, 0), Is.EqualTo(frame.Length - NetworkConstants.LENGTH_PREFIX_SIZE));
        }

        [Test]
        public void SampleMessages_CoverEveryMessageType()
        {
            var sampled = SampleMessages().Select(m => m.MessageType);

            Assert.That(sampled, Is.EquivalentTo(Enum.GetValues<eMessageType>()));
        }

        [Test]
        public void CreateMessage_ReturnsMatchingTypeForEveryMessageType()
        {
            foreach (var messageType in Enum.GetValues<eMessageType>())
                Assert.That(MessageSerializer.CreateMessage(messageType).MessageType, Is.EqualTo(messageType));
        }

        [Test]
        public void RoundTrip_LevelWon_KeepsFields()
        {
            var message = (LevelWonMessage)SampleMessages().First(m => m.MessageType == eMessageType.LevelWon);

            var copy = (LevelWonMessage)MessageSerializer.Deserialize(GetBody(MessageSerializer.Serialize(message)));

            Assert.That(copy.WinningTeam, Is.EqualTo(message.WinningTeam));
            Assert.That(copy.RedScore, Is.EqualTo(message.RedScore));
            Assert.That(copy.BlueScore, Is.EqualTo(message.BlueScore));
            Assert.That(copy.MsRemaining, Is.EqualTo(message.MsRemaining));
            Assert.That(copy.Results, Is.EqualTo(message.Results));
        }

        [Test]
        public void RoundTrip_Snapshot_KeepsFields()
        {
            var message = (SnapshotMessage)SampleMessages().First(m => m.MessageType == eMessageType.Snapshot);

            var copy = (SnapshotMessage)MessageSerializer.Deserialize(GetBody(MessageSerializer.Serialize(message)));

            Assert.That(copy.Tick, Is.EqualTo(message.Tick));
            Assert.That(copy.RedScore, Is.EqualTo(message.RedScore));
            Assert.That(copy.BlueScore, Is.EqualTo(message.BlueScore));
            Assert.That(copy.Pacs, Is.EqualTo(message.Pacs));
            Assert.That(copy.Ghosts, Is.EqualTo(message.Ghosts));
            Assert.That(copy.Flags, Is.EqualTo(message.Flags));
            Assert.That(copy.PowerUps, Is.EqualTo(message.PowerUps));
        }

        [Test]
        public void RoundTrip_LevelStart_KeepsWallColor()
        {
            var message = new LevelStartMessage { WallColor = FastColor.FromArgb(200, 10, 20, 30) };

            var copy = (LevelStartMessage)MessageSerializer.Deserialize(GetBody(MessageSerializer.Serialize(message)));

            Assert.That(copy.WallColor.PackedValue, Is.EqualTo(message.WallColor.PackedValue));
        }

        [Test]
        public void RoundTrip_LevelStart_KeepsWallFillColor()
        {
            var message = new LevelStartMessage { WallFillColor = FastColor.FromArgb(128, 40, 50, 60) };

            var copy = (LevelStartMessage)MessageSerializer.Deserialize(GetBody(MessageSerializer.Serialize(message)));

            Assert.That(copy.WallFillColor.PackedValue, Is.EqualTo(message.WallFillColor.PackedValue));
        }

        [Test]
        public void RoundTrip_LevelStart_KeepsBackgroundColor()
        {
            var message = new LevelStartMessage { BackgroundColor = FastColor.FromArgb(255, 15, 51, 51) };

            var copy = (LevelStartMessage)MessageSerializer.Deserialize(GetBody(MessageSerializer.Serialize(message)));

            Assert.That(copy.BackgroundColor.PackedValue, Is.EqualTo(message.BackgroundColor.PackedValue));
        }

        /// <summary>
        /// Fails if a property is added to GameConfiguration but not to NetworkHelper.
        /// </summary>
        [Test]
        public void RoundTrip_Welcome_KeepsEveryGameConfigurationProperty()
        {
            var config = new GameConfiguration();
            var properties = typeof(GameConfiguration).GetProperties();

            // give every property a distinct non-default value
            for (var i = 0; i < properties.Length; i++)
            {
                var value = 1000 + i;
                properties[i].SetValue(config, Convert.ChangeType(value, properties[i].PropertyType));
            }

            var message = new WelcomeMessage { GameConfig = config };
            var copy = (WelcomeMessage)MessageSerializer.Deserialize(GetBody(MessageSerializer.Serialize(message)));

            foreach (var property in properties)
                Assert.That(property.GetValue(copy.GameConfig), Is.EqualTo(property.GetValue(config)), property.Name);
        }

        [Test]
        public void Deserialize_UnknownMessageType_Throws()
        {
            Assert.Throws<InvalidDataException>(() => MessageSerializer.Deserialize([255]));
        }

        [Test]
        public void Deserialize_EmptyBody_Throws()
        {
            Assert.Throws<InvalidDataException>(() => MessageSerializer.Deserialize([]));
        }

        [Test]
        public void Deserialize_TruncatedBody_Throws()
        {
            var body = GetBody(MessageSerializer.Serialize(new PlayerJoinedMessage { PlayerId = 1, Name = "Clyde", Team = eTeam.Red }));

            Assert.Throws<InvalidDataException>(() => MessageSerializer.Deserialize(body[..^1]));
        }

        [Test]
        public void Deserialize_ExtraBytes_Throws()
        {
            var body = GetBody(MessageSerializer.Serialize(new PlayerLeftMessage { PlayerId = 1 }));

            Assert.Throws<InvalidDataException>(() => MessageSerializer.Deserialize([.. body, 0]));
        }

        [Test]
        public void Deserialize_NegativeListCount_Throws()
        {
            // IsCatchUp, then the cell count
            byte[] body = [(byte)eMessageType.CellsChanged, 0, .. BitConverter.GetBytes(-1)];

            Assert.Throws<InvalidDataException>(() => MessageSerializer.Deserialize(body));
        }
    }
}
