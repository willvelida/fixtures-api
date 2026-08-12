namespace FixturesApi.Data;

public record Match(string Season, int Matchday, string Date, string Home, string Away,
                    int? HomeScore, int? AwayScore, bool Played);

public record Standing(int Position, string Team, int Played, int Won, int Drawn, int Lost,
                       int Gf, int Ga, int Gd, int Points);

public record SeedData(List<Match> Matches, List<Standing> Standings);

public record TeamSummary(string Team, int Played, int Won, int Drawn, int Lost,
                          int GoalsFor, int GoalsAgainst, int GoalDifference, int Points);
