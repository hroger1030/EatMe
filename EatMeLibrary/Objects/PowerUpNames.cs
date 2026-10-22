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
                ePowerUp.Galaxian => "Galaxian",
                ePowerUp.Bell => "Bell",
                ePowerUp.Key => "Key",
                ePowerUp.Pretzel => "Pretzel",
                ePowerUp.Pear => "Pear",
                ePowerUp.Banana => "Banana",
                ePowerUp.Mushroom => "Mushroom",
                ePowerUp.PowerPill => "Power Pill",
                ePowerUp.FireFlower => "Fire Flower",
                ePowerUp.Starman => "Starman",
                ePowerUp.PowBlock => "POW Block",
                ePowerUp.SuperLeaf => "Super Leaf",
                ePowerUp.Hammer => "Hammer",
                ePowerUp.Triforce => "Triforce",
                ePowerUp.HeartContainer => "Heart Container",
                ePowerUp.WhiteSword => "White Sword",
                ePowerUp.EnergyTank => "Energy Tank",
                ePowerUp.Reinforcements => "Reinforcements",
                ePowerUp.ScrewAttack => "Screw Attack",
                ePowerUp.HolyWater => "Holy Water",
                ePowerUp.SpreadGun => "Spread Gun",
                ePowerUp.BlueShell => "Blue Shell",
                ePowerUp.Tetromino => "Tetromino",
                ePowerUp.BFG => "BFG",
                ePowerUp.Bomb => "Bomb",
                ePowerUp.PepperShaker => "Pepper Shaker",
                ePowerUp.PokeBall => "Poke Ball",
                _ => throw new ArgumentOutOfRangeException(nameof(item), item, "Not a single bonus item"),
            };
        }
    }
}
