namespace DDUP
{
	public record ItemViewRow(
	int Rating, int Sides, string Name, string Location, string Quality, string Type, string Position,
	int Level, int MaxLevel,
	int HHP, int HDmg, int HSpd, int HRate, int Ab1, int Ab2,
	int THP, int TDmg, int TRange, int TRate,
	int RG, int RP, int RF, int RL, int Idx);
}
