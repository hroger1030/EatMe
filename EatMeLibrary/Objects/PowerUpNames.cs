using System;

namespace EatMeLibrary
{
    /// <summary>
    /// The name each bonus item is shown by, as on the attract screen. Printable ASCII only, since sprite fonts are.
    /// </summary>
    public static class PowerUpNames
    {
        public static string GetName(ePowerUp item)
        {
            return item switch
            {
                ePowerUp.Cherry => "Cherry",
                ePowerUp.Strawberry => "Strawberry",
                ePowerUp.Orange => "Orange",
                ePowerUp.Apple => "Apple",
                ePowerUp.Melon => "Melon",
                ePowerUp.Solarian => "Solarian",
                ePowerUp.Bell => "Bell",
                ePowerUp.Key => "Key",
                ePowerUp.Pretzel => "Pretzel",
                ePowerUp.Pear => "Pear",
                ePowerUp.Banana => "Banana",
                ePowerUp.Mushroom => "Mushroom",
                ePowerUp.PowerPill => "Power Pill",
                ePowerUp.SunFlower => "Sun Flower",
                ePowerUp.Starman => "Starman",
                ePowerUp.PowBlock => "POW Block",
                ePowerUp.SuperLeaf => "Super Leaf",
                ePowerUp.HammerTime => "Hammer Time",
                ePowerUp.Triangleforce => "Triangle Force",
                ePowerUp.HeartContainer => "Heart Container",
                ePowerUp.WhiteSword => "White Sword",
                ePowerUp.EnergyTank => "Energy Tank",
                ePowerUp.Reinforcements => "Reinforcements",
                ePowerUp.RollingThunder => "Rolling Thunder",
                ePowerUp.HolyWater => "Holy Water",
                ePowerUp.SpreadGun => "Spread Gun",
                ePowerUp.CobaltShell => "Cobalt Shell",
                ePowerUp.Skyblock => "Skyblock",
                ePowerUp.BFG => "BFG",
                ePowerUp.StunBomb => "Stun Bomb",
                ePowerUp.PepperShaker => "Pepper Shaker",
                ePowerUp.MonsterBall => "Monster Ball",
                _ => throw new ArgumentOutOfRangeException(nameof(item), item, "Not a single bonus item"),
            };
        }
    }
}
