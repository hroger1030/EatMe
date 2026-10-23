using System;

namespace EatMeLibrary
{
    /// <summary>
    /// Short, friendly descriptions for the attract screen: how each ghost behaves and what each power up does, in
    /// plain words rather than numbers. Printable ASCII only, since sprite fonts are.
    /// </summary>
    public static class AttractBlurbs
    {
        public static string GetGhostBlurb(eGhostBehavior behavior)
        {
            return behavior switch
            {
                eGhostBehavior.Normal => "Chases the nearest pac it can see, flag carriers first.",
                eGhostBehavior.Hunter => "Spots you from far away and never lets go of its prey.",
                eGhostBehavior.Ambusher => "Runs on ahead to cut you off where you're going.",
                eGhostBehavior.Ditherer => "Comes close, loses its nerve, runs home, then tries again.",
                eGhostBehavior.FlagHound => "Can smell a flag carrier a mile away, and patrols the flags.",
                eGhostBehavior.Guard => "Sticks close to home and won't chase you far.",
                eGhostBehavior.Wanderer => "Slow and short sighted, just drifting along.",
                eGhostBehavior.Sprinter => "Very fast, but tires quickly and needs a rest.",
                eGhostBehavior.Lurker => "Hides at a crossroads, then charges when you pass.",
                eGhostBehavior.Bully => "Picks on the smallest pac it can see.",
                eGhostBehavior.Glutton => "Goes after the biggest pac it can see.",
                eGhostBehavior.Grudge => "Never forgets who ate it, and comes looking for them.",
                eGhostBehavior.Elroy => "Gets faster and faster as a team gets close to winning.",
                eGhostBehavior.SlowThinker => "Always a step behind, heading to where you used to be.",
                eGhostBehavior.StraightLiner => "Charges straight at you, and keeps bumping into walls.",
                eGhostBehavior.Shy => "Freezes when you look at it, and creeps up when you don't.",
                eGhostBehavior.Copycat => "Copies every move you make.",
                eGhostBehavior.Sleeper => "Stops for a quick nap every now and then.",
                eGhostBehavior.Confused => "Can't decide who to chase, so keeps changing its mind.",
                eGhostBehavior.Roamer => "Too dizzy to know where it's going, but it'll bite anything that wanders too close.",
                eGhostBehavior.Sidekick => "Tags along with its ghost friends and joins the chase.",
                _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, "No blurb for this ghost"),
            };
        }

        public static string GetPowerUpBlurb(ePowerUp item)
        {
            return item switch
            {
                ePowerUp.Cherry => "Drop a decoy twin that the ghosts chase instead of you.",
                ePowerUp.Strawberry => "Every dot makes you grow three times as much.",
                ePowerUp.Orange => "Full of vitamins, so you never go hungry.",
                ePowerUp.Apple => "An apple a day keeps the ghosts away.",
                ePowerUp.Melon => "Press action button to roll through walls, stunning anyone in your way.",
                ePowerUp.Solarian => "Warp speed! Zoom round the maze much faster.",
                ePowerUp.Bell => "Ring the dinner bell, and every ghost goes twice as fast!",
                ePowerUp.Key => "Locks the enemy's spawn, so fallen pacs can't come back.",
                ePowerUp.Pretzel => "Ties everyone near you in knots, slowing them to half speed.",
                ePowerUp.Pear => "Press action button to teleport around the maze.",
                ePowerUp.Banana => "Press action button to drop a peel that stuns whoever steps on it.",
                ePowerUp.Mushroom => "Become huge and smash right through the walls.",
                ePowerUp.PowerPill => "Turn the tables and eat the ghosts.",
                ePowerUp.SunFlower => "Press action button to throw fireballs.",
                ePowerUp.Starman => "Invincible! Run faster and gobble up anything you touch.",
                ePowerUp.PowBlock => "Press action button to stun everything around you.",
                ePowerUp.SuperLeaf => "Float right through walls, ghosts and enemies.",
                ePowerUp.HammerTime => "Swing a hammer that flattens enemies and walls.",
                ePowerUp.Triangleforce => "Brings your flags home and shields them from thieves.",
                ePowerUp.HeartContainer => "Heals you up to full size and keeps hunger away.",
                ePowerUp.WhiteSword => "Press action button to throw a sword.",
                ePowerUp.EnergyTank => "Run faster and grow to twice your normal limit.",
                ePowerUp.Reinforcements => "Reinforcements! Your fallen teammates have no respawn timer.",
                ePowerUp.RollingThunder => "Spin into a whirlwind that wrecks anything you touch.",
                ePowerUp.HolyWater => "Leave a trail of holy fire behind you. Don't touch it!",
                ePowerUp.SpreadGun => "Fires stunning shots in every direction, all by itself.",
                ePowerUp.CobaltShell => "Press the action button to send a seeker shell after the enemy flag carrier.",
                ePowerUp.Skyblock => "Leave a trail of blocks behind you to seal the way.",
                ePowerUp.BFG => "Press the action button for a huge blast that wipes out EVERYTHING nearby",
                ePowerUp.StunBomb => "The bomb explodes and stuns all nearby enemies.",
                ePowerUp.PepperShaker => "Ghosts that touch you get stunned.",
                ePowerUp.MonsterBall => "Press action button to throw a ball that catches the nearest ghost.",
                _ => throw new ArgumentOutOfRangeException(nameof(item), item, "Not a single bonus item"),
            };
        }
    }
}
