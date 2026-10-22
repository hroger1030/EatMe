using System.Collections.Generic;
using System.IO;

namespace EatMeLibrary
{
    /// <summary>
    /// Server to client: where everything is right now. Sent ClientUpdatesPerSecond times a second.
    /// </summary>
    public class SnapshotMessage : INetworkMessage
    {
        public eMessageType MessageType => eMessageType.Snapshot;

        /// <summary>
        /// Server tick this snapshot was taken on; clients drop snapshots older than the last one seen.
        /// </summary>
        public uint Tick { get; set; }
        public int RedScore { get; set; }
        public int BlueScore { get; set; }
        public List<PacState> Pacs { get; set; } = [];
        public List<GhostState> Ghosts { get; set; } = [];
        public List<FlagState> Flags { get; set; } = [];
        public List<PowerUpState> PowerUps { get; set; } = [];
        public List<WeaponEffectState> WeaponEffects { get; set; } = [];

        public void Write(BinaryWriter writer)
        {
            writer.Write(Tick);
            writer.Write(RedScore);
            writer.Write(BlueScore);

            writer.Write(Pacs.Count);
            foreach (var pac in Pacs)
                pac.Write(writer);

            writer.Write(Ghosts.Count);
            foreach (var ghost in Ghosts)
                ghost.Write(writer);

            writer.Write(Flags.Count);
            foreach (var flag in Flags)
                flag.Write(writer);

            writer.Write(PowerUps.Count);
            foreach (var powerUp in PowerUps)
                powerUp.Write(writer);

            writer.Write(WeaponEffects.Count);
            foreach (var weaponEffect in WeaponEffects)
                weaponEffect.Write(writer);
        }

        public void Read(BinaryReader reader)
        {
            Tick = reader.ReadUInt32();
            RedScore = reader.ReadInt32();
            BlueScore = reader.ReadInt32();

            var pacCount = NetworkHelper.ReadCount(reader);
            Pacs = [];
            for (var i = 0; i < pacCount; i++)
                Pacs.Add(PacState.Read(reader));

            var ghostCount = NetworkHelper.ReadCount(reader);
            Ghosts = [];
            for (var i = 0; i < ghostCount; i++)
                Ghosts.Add(GhostState.Read(reader));

            var flagCount = NetworkHelper.ReadCount(reader);
            Flags = [];
            for (var i = 0; i < flagCount; i++)
                Flags.Add(FlagState.Read(reader));

            var powerUpCount = NetworkHelper.ReadCount(reader);
            PowerUps = [];
            for (var i = 0; i < powerUpCount; i++)
                PowerUps.Add(PowerUpState.Read(reader));

            var weaponEffectCount = NetworkHelper.ReadCount(reader);
            WeaponEffects = [];
            for (var i = 0; i < weaponEffectCount; i++)
                WeaponEffects.Add(WeaponEffectState.Read(reader));
        }
    }
}
