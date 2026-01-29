using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using HarmonyLib;
using System.Text;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.Managers;

[HarmonyPatch]
internal static class EndGameManagerPatch
{
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    [HarmonyPostfix]
    private static void EndGameManager_SetEverythingUp_Postfix(EndGameManager __instance)
    {
        Logger_.LogHeader($"Game Has Ended - {Enum.GetName(typeof(MapNames), GameState.GetActiveMapId)}/{GameState.GetActiveMapId}", "GamePlayManager");

        Logger_.LogHeader("Game Summary Start", "GameSummary");

        GameObject SummaryObj = UnityEngine.Object.Instantiate(__instance.WinText.gameObject, __instance.WinText.transform.parent.transform);
        SummaryObj.name = "SummaryObj (TMP)";
        SummaryObj.transform.SetSiblingIndex(0);
        Camera localCamera;
        if (HudManager.InstanceExists)
        {
            localCamera = HudManager.Instance.GetComponentInChildren<Camera>();
        }
        else
        {
            localCamera = Camera.main;
        }

        SummaryObj.transform.position = AspectPosition.ComputeWorldPosition(localCamera, AspectPosition.EdgeAlignments.LeftTop, new Vector3(1f, 0.2f, -5f));
        SummaryObj.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        TextMeshPro SummaryText = SummaryObj.GetComponent<TextMeshPro>();
        if (SummaryText != null)
        {
            SummaryText.autoSizeTextContainer = false;
            SummaryText.enableAutoSizing = false;
            SummaryText.lineSpacing = -25f;
            SummaryText.alignment = TextAlignmentOptions.TopLeft;
            SummaryText.color = Color.white;

            NetworkedPlayerInfo[] playersData = GameData.Instance.AllPlayers
                .ToArray()
                .OrderBy(pd => pd.Disconnected)  // Disconnected players last
                .ThenBy(pd => pd.IsDead)
                .ThenBy(pd => !pd.Role.IsImpostor)
                .ToArray();        // Dead players after live players

            string winTeam;
            string winTag;
            string winColor;

            switch (EndGameResult.CachedGameOverReason)
            {
                case GameOverReason.CrewmatesByTask:
                    winTeam = Translator.GetString(StringNames.Crewmates);
                    winTag = Translator.GetString("Game.Summary.Result.TasksCompletion");
                    winColor = "#8cffff";
                    break;
                case GameOverReason.CrewmatesByVote:
                    winTeam = Translator.GetString(StringNames.Crewmates);
                    winTag = Translator.GetString("Game.Summary.Result.ImpostersVotedOut");
                    winColor = "#8cffff";
                    break;
                case GameOverReason.ImpostorDisconnect:
                    winTeam = Translator.GetString(StringNames.Crewmates);
                    winTag = Translator.GetString("Game.Summary.Result.ImpostorsDisconnected");
                    winColor = "#8cffff";
                    break;
                case GameOverReason.ImpostorsByKill:
                    winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                    winTag = Translator.GetString("Game.Summary.Result.CrewOutnumbered");
                    winColor = "#f00202";
                    break;
                case GameOverReason.ImpostorsBySabotage:
                    winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                    winTag = Translator.GetString("Game.Summary.Result.Sabotage");
                    winColor = "#f00202";
                    break;
                case GameOverReason.ImpostorsByVote:
                    winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                    winTag = Translator.GetString("Game.Summary.Result.CrewOutnumbered");
                    winColor = "#f00202";
                    break;
                case GameOverReason.CrewmateDisconnect:
                    winTeam = Translator.GetString(StringNames.ImpostorsCategory);
                    winTag = Translator.GetString("Game.Summary.Result.CrematesDisconnected");
                    winColor = "#f00202";
                    break;

                case GameOverReason.HideAndSeek_CrewmatesByTimer:
                    winTeam = Translator.GetString("Game.Summary.Hiders");
                    winTag = Translator.GetString("Game.Summary.Result.TimeOut");
                    winColor = "#8cffff";
                    break;
                case GameOverReason.HideAndSeek_ImpostorsByKills:
                    winTeam = Translator.GetString("Game.Summary.Seekers");
                    winTag = Translator.GetString("Game.Summary.Result.NoSurvivors");
                    winColor = "#f00202";
                    break;

                default:
                    winTeam = "Unknown";
                    winTag = "Unknown";
                    winColor = "#ffffff";
                    break;
            }

            Logger_.Log($"{winTeam}: {winTag}", "GameSummary");

            string SummaryHeader = $"<align=\"center\"><size=150%>   {Translator.GetString("GameSummary")}</size></align>";
            SummaryHeader += $"\n\n<size=90%><color={winColor}>{winTeam} {Translator.GetString("Game.Summary.Won")}</color></size>" +
                $"\n<size=60%>\n{Translator.GetString("Game.Summary.By")} {winTag}</size>";

            StringBuilder sb = new StringBuilder();

            foreach (var data in playersData)
            {
                var name = $"<color={Utils.Color32ToHex(Palette.PlayerColors[data.DefaultOutfit.ColorId])}>{data.BetterData().RealName}</color>";
                string playerTheme(string text) => $"<color={Utils.GetTeamHexColor(data.Role.TeamType)}>{text}</color>";

                string roleInfo;
                if (data.Role.IsImpostor)
                {
                    roleInfo = $"({playerTheme(data.RoleType.GetRoleName())}) → {playerTheme($"{Translator.GetString("Kills")}: {data.BetterData().RoleInfo.Kills}")}";
                }
                else
                {
                    roleInfo = $"({playerTheme(data.RoleType.GetRoleName())}) → {playerTheme($"{Translator.GetString("Tasks")}: {data.Tasks.WhereIl2Cpp(task => task.Complete).Count}/{data.Tasks.Count}")}";
                }

                string deathReason;
                if (data.Disconnected)
                {
                    deathReason = $"『<color=#838383><b>{Translator.GetString("DC")}</b></color>』";
                }
                else if (!data.IsDead)
                {
                    deathReason = $"『<color=#80ff00><b>{Translator.GetString("Alive")}</b></color>』";
                }
                else if (data.IsDead)
                {
                    deathReason = $"『<color=#ff0600><b>{Translator.GetString("Dead")}</b></color>』";
                }
                else
                {
                    deathReason = $"『<color=#838383<b>Unknown</b></color>』";
                }

                Logger_.Log($"{name} {roleInfo} {deathReason}", "GameSummary");

                sb.AppendLine($"- {name} {roleInfo} {deathReason}\n");
            }

            SummaryText.text = $"{SummaryHeader}\n\n<size=58%>{sb}</size>";
            Logger_.LogHeader("Game Summary End", "GameSummary");
        }
    }
}
