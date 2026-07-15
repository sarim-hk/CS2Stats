using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CS2Stats {

    public partial class CS2Stats {

        private TeamInfo? GetTeamInfoByTeamNum(int? teamNum) {
            if (this.Match != null && teamNum != null) {
                foreach (string teamID in this.Match.StartingPlayers.Keys) {
                    TeamInfo teamInfo = this.Match.StartingPlayers[teamID];

                    if (teamInfo.Side == teamNum) {
                        return teamInfo;
                    }
                }
            }

            return null;
        }

        private static LiveData GetLiveMatchData() {

            List<LivePlayer> players = [];

            foreach (CCSPlayerController playerController in Utilities.GetPlayers()) {
                if ((playerController.ActionTrackingServices != null) &&
                    (!playerController.IsBot && playerController.IsValid &&
                    (playerController.Team == CsTeam.Terrorist || playerController.Team == CsTeam.CounterTerrorist))) {

                    LivePlayer livePlayer = new() {
                        PlayerID = playerController.SteamID,
                        Kills = playerController.ActionTrackingServices.MatchStats.Kills,
                        Assists = playerController.ActionTrackingServices.MatchStats.Assists,
                        Deaths = playerController.ActionTrackingServices.MatchStats.Deaths,
                        ADR = (GetGameRules().TotalRoundsPlayed != 0) ? (playerController.ActionTrackingServices.MatchStats.Damage / GetGameRules().TotalRoundsPlayed) : 0,
                        Health = playerController.PlayerPawn.Value?.Health ?? 0,
                        Money = playerController.InGameMoneyServices?.Account ?? 0,
                        Side = playerController.TeamNum,
                    };
                    players.Add(livePlayer);

                }
            }

            LiveStatus status = new() {
                BombStatus = GetGameRules().BombPlanted switch { true => 1, false => GetGameRules().BombDefused ? 2 : 0 },
                MapName = Server.MapName,
                TScore = GetCSTeamScore(2),
                CTScore = GetCSTeamScore(3)
            };

            LiveData liveData = new() {
                Players = players,
                Status = status
            };
                
            return liveData;
        }

        private static CCSGameRules GetGameRules() {
            // thanks to bober https://discord.com/channels/1160907911501991946/1160925208203493468/1173658546387292160

            return Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
        }

        private static string GenerateTeamID(HashSet<ulong> teamPlayerIDs, ILogger Logger) {
            string teamID = BitConverter.ToString(
                MD5.HashData(
                    Encoding.UTF8.GetBytes(
                        string.Join("", teamPlayerIDs.OrderBy(id => id))
                    )
                )
            ).Replace("-", "");
            Logger.LogInformation($"[GenerateTeamID] Team: {string.Join(", ", teamPlayerIDs)} are {teamID}");
            return teamID;
        }

        private static int GetCSTeamScore(int teamNum) {
            // thanks to switz https://discord.com/channels/1160907911501991946/1160925208203493468/1170817201473855619

            IEnumerable<CCSTeam> teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (CCSTeam teamManager in teamManagers) {
                if (teamNum == teamManager.TeamNum) {
                    return teamManager.Score;
                }
            }

            return 0;
        }

        private static CCSTeam? GetCSTeamByTeamNum(int teamNum) {
            // thanks to switz https://discord.com/channels/1160907911501991946/1160925208203493468/1170817201473855619

            IEnumerable<CCSTeam> teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (CCSTeam teamManager in teamManagers) {
                if (teamNum == teamManager.TeamNum) {
                    return teamManager;
                }
            }

            return null;
        }

        private void SwapTeamsIfNeeded() {
            if (this.Match != null && this.Match.TeamsNeedSwapping) {
                foreach (string teamID in this.Match.StartingPlayers.Keys) {
                    Logger.LogInformation($"[SwapTeamsIfNeeded] Swapping team for teamID {teamID}.");
                    this.Match.StartingPlayers[teamID].SwapSides();
                }
                this.Match.TeamsNeedSwapping = false;
                Logger.LogInformation("[SwapTeamsIfNeeded] Setting teamsNeedSwapping to false.");
            }
        }

        public void StartDemo(ILogger Logger) {

            string demoDirectoryPath = Path.Combine(Server.GameDirectory, "csgo", "CS2Stats");
            if (!Directory.Exists(demoDirectoryPath)) {
                Directory.CreateDirectory(demoDirectoryPath);
            }

            string demoFileName;
            if (this.Match != null) {
                demoFileName = this.Match.MatchName + ".dem";
            } else {
                demoFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + Server.MapName + ".dem";
            }

            string demoPath = Path.Combine(demoDirectoryPath, demoFileName);

            this.StopDemo(Logger);

            Server.ExecuteCommand($"tv_record \"{demoPath}\"");
            Logger.LogInformation($"[StartDemo] Started recording demo: {demoPath}");
        }

        public void StopDemo(ILogger Logger) {
            Server.ExecuteCommand("tv_stoprecord");
            Logger.LogInformation("[StopDemo] Stopped recording demo.");
        }

    }

}
