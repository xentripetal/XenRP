﻿using System.Linq;
using GTANetworkAPI;
using XenRP.character;
using XenRP.globals;
using XenRP.Jobs;
using XenRP.Jobs.FastFood;
using XenRP.messages.error;
using XenRP.messages.general;
using XenRP.messages.information;

namespace XenRP.jobs {
    public class Job : Script {
        public Job() {
            foreach (var job in Constants.JOB_PICK_LIST) {
                // Create the label for the command
                NAPI.TextLabel.CreateTextLabel("/" + Commands.COM_JOB, job.position, 10.0f, 0.5f, 4,
                    new Color(190, 235, 100), false, 0);
                NAPI.TextLabel.CreateTextLabel(GenRes.job_help,
                    new Vector3(job.position.X, job.position.Y, job.position.Z - 0.1f), 10.0f, 0.5f, 4,
                    new Color(255, 255, 255), false, 0);

                if (job.blip > 0) {
                    // Create the blip for the job
                    var jobBlip = NAPI.Blip.CreateBlip(job.position);
                    jobBlip.Name = job.name;
                    jobBlip.ShortRange = true;
                    jobBlip.Sprite = job.blip;
                }
            }
        }

        public static int GetJobPoints(Client player, int job) {
            string jobPointsString = player.GetData(EntityData.PLAYER_JOB_POINTS);
            return int.Parse(jobPointsString.Split(',')[job]);
        }

        public static void SetJobPoints(Client player, int job, int points) {
            string jobPointsString = player.GetData(EntityData.PLAYER_JOB_POINTS);
            var jobPointsArray = jobPointsString.Split(',');
            jobPointsArray[job] = points.ToString();
            jobPointsString = string.Join(",", jobPointsArray);
            player.SetData(EntityData.PLAYER_JOB_POINTS, jobPointsString);
        }

        private void ShowPlayerJobCommands(Client player) {
            // Get the player's current job
            int job = player.GetData(EntityData.PLAYER_JOB);

            // Get the commands from the job
            var commands = string.Join(", ", Constants.JOB_COMMANDS[job]);

            // Send the message to the player
            var jobDescription = Constants.JOB_LIST[job].descriptionMale;
            var message = string.Format(InfoRes.commands_from, jobDescription);
            player.SendChatMessage(Constants.COLOR_INFO + message);
            player.SendChatMessage(Constants.COLOR_HELP + commands);
        }

        private bool IsPlayerOnWorkPlace(Client player) {
            var onWorkPlace = false;

            int job = player.GetData(EntityData.PLAYER_JOB);
            int faction = player.GetData(EntityData.PLAYER_FACTION);

            if (job > 0) {
                // Check if it's close to the point where he got the job
                onWorkPlace =
                    player.Position.DistanceTo(Constants.JOB_PICK_LIST.Where(j => j.job == job).First().position) <
                    2.0f;
            }
            else if (faction > 0) {
                // Store the Vector where the lockers are located
                Vector3 lockers = null;

                switch (faction) {
                    case Constants.FACTION_POLICE:
                        lockers = new Vector3(450.8223f, -992.0941f, 30.68958f);
                        break;
                    case Constants.FACTION_EMERGENCY:
                        lockers = new Vector3(268.8305f, -1363.443f, 24.53779f);
                        break;
                    case Constants.FACTION_SHERIFF:
                        var paletoLockers = new Vector3(-448.7167f, 6011.534f, 31.71639f);
                        var sandyLockers = new Vector3(1852.255f, 3689.962f, 34.26704f);

                        // Get the closest lockers
                        lockers = player.Position.DistanceTo(paletoLockers) < player.Position.DistanceTo(sandyLockers)
                            ? paletoLockers
                            : sandyLockers;
                        break;
                }

                onWorkPlace = lockers != null && player.Position.DistanceTo(lockers) < 5.0f;
            }

            return onWorkPlace;
        }

        [Command(Commands.COM_JOB, Commands.HLP_JOB_COMMAND)]
        public void JobCommand(Client player, string action) {
            int faction = player.GetData(EntityData.PLAYER_FACTION);
            int job = player.GetData(EntityData.PLAYER_JOB);

            switch (action.ToLower()) {
                case Commands.ARG_INFO:
                    foreach (var jobPick in Constants.JOB_PICK_LIST)
                        if (player.Position.DistanceTo(jobPick.position) < 1.5f) {
                            player.SendChatMessage(Constants.COLOR_INFO + jobPick.description);
                            break;
                        }

                    break;
                case Commands.ARG_ACCEPT:
                    if (faction > 0 && faction < Constants.LAST_STATE_FACTION)
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_job_state_faction);
                    else if (job > 0)
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_has_job);
                    else
                        foreach (var jobPick in Constants.JOB_PICK_LIST)
                            if (player.Position.DistanceTo(jobPick.position) < 1.5f) {
                                player.SetData(EntityData.PLAYER_JOB, jobPick.job);
                                player.SetData(EntityData.PLAYER_EMPLOYEE_COOLDOWN, 5);
                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.job_accepted);
                                break;
                            }

                    break;
                case Commands.ARG_LEAVE:
                    // Get the hours spent in the current job
                    int employeeCooldown = player.GetData(EntityData.PLAYER_EMPLOYEE_COOLDOWN);

                    if (employeeCooldown > 0) {
                        var message = string.Format(ErrRes.employee_cooldown, employeeCooldown);
                        player.SendChatMessage(Constants.COLOR_ERROR + message);
                    }
                    else if (player.GetData(EntityData.PLAYER_JOB_RESTRICTION) > 0) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_job_restriction);
                    }
                    else if (job == 0) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_no_job);
                    }
                    else {
                        player.SetData(EntityData.PLAYER_JOB, 0);
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.job_left);
                    }

                    break;
                case Commands.ARG_HELP:
                    if (player.GetData(EntityData.PLAYER_JOB) == 0)
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_no_job);
                    else
                        ShowPlayerJobCommands(player);
                    break;
                default:
                    player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_JOB_COMMAND);
                    break;
            }
        }

        [Command(Commands.COM_DUTY)]
        public void DutyCommand(Client player) {
            // We get the sex, job and faction from the player
            int playerSex = player.GetData(EntityData.PLAYER_SEX);
            int playerJob = player.GetData(EntityData.PLAYER_JOB);
            int playerFaction = player.GetData(EntityData.PLAYER_FACTION);

            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (playerJob == 0 && playerFaction == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_no_job);
            }
            else if (!IsPlayerOnWorkPlace(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_work_place);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 1) {
                // Populate player's clothes
                Customization.ApplyPlayerClothes(player);

                // We set the player off duty
                player.SetData(EntityData.PLAYER_ON_DUTY, 0);

                // Notification sent to the player
                player.SendNotification(InfoRes.player_free_time);
            }
            else {
                // Dress the player with the uniform
                foreach (var uniform in Constants.UNIFORM_LIST)
                    if (uniform.type == 0 && uniform.factionJob == playerFaction && playerSex == uniform.characterSex)
                        player.SetClothes(uniform.uniformSlot, uniform.uniformDrawable, uniform.uniformTexture);
                    else if (uniform.type == 1 && uniform.factionJob == playerJob && playerSex == uniform.characterSex)
                        player.SetClothes(uniform.uniformSlot, uniform.uniformDrawable, uniform.uniformTexture);

                // We set the player on duty
                player.SetData(EntityData.PLAYER_ON_DUTY, 1);

                // Notification sent to the player
                player.SendNotification(InfoRes.player_on_duty);
            }
        }

        [Command(Commands.COM_ORDERS)]
        public void OrdersCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (player.GetData(EntityData.PLAYER_DELIVER_ORDER) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.order_delivering);
            }
            else {
                if (player.GetData(EntityData.PLAYER_JOB) == Constants.JOB_FASTFOOD) {
                    // Get the fastfood deliverer orders
                    FastFood.CheckFastfoodOrders(player);
                    return;
                }

                if (player.GetData(EntityData.PLAYER_JOB) == Constants.JOB_TRUCKER) Trucker.CheckTruckerOrders(player);
            }
        }
    }
}