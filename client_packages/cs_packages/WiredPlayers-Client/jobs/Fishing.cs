using RAGE;
using RAGE.Game;
using System;

namespace WiredPlayers_Client.jobs
{
    class Fishing : Events.Script
    {
        private static int width;
        private static int height;
        private static int fishingSuccess;
        private static float fishingBarPosition;
        private static float fishingAchieveStart;
        private static float fishingBarMin;
        private static float fishingBarMax;
        private static bool movementRight;
        public static int fishingState;

        public Fishing()
        {
            movementRight = true;
            fishingBarMin = 0.0f;
            fishingBarMax = 0.0f;
            fishingAchieveStart = 0.0f;
            Graphics.GetActiveScreenResolution(ref width, ref height);
        }

        public static void DrawFishingMinigame()
        {
            if(Input.IsDown(0x45))
            {
                switch(fishingState)
                {
                    case 1:
                        // Start fishing
                        fishingState = 2;
                        fishingBarPosition = width - 224.0f;
                        Events.CallRemote("startFishingTimer");
                        break;
                    case 2:
                        // Player didn't catch any fish
                        fishingState = -1;
                        Events.CallRemote("fishingCanceled");
                        break;
                    case 3:
                        if (fishingBarPosition > fishingAchieveStart && fishingBarPosition < fishingAchieveStart + 15.0f)
                        {
                            // Valid catch
                            fishingSuccess++;

                            if (fishingSuccess == 3)
                            {
                                // Fishing succeed
                                fishingState = -1;
                                Events.CallRemote("fishingSuccess");
                            }
                            else
                            {
                                // Generate the new bars
                                movementRight = true;
                                fishingBarPosition = width - 224.0f;

                                Random random = new Random();
                                fishingAchieveStart = (random.Next() * 390.0f) + fishingBarMin;
                            }
                        }
                        else
                        {
                            // Player failed catching
                            fishingState = -1;
                            Events.CallRemote("fishingCanceled");
                        }
                        break;
                }

                // Don't display anything
                return;
            }

            if (fishingState == 3)
            {
                // Draw the minigame bar
                Graphics.DrawRect(width - 425.0f, height - 40.0f, 400.0f, 25.0f, 0, 0, 0, 200, 0);

                // Draw the success bar
                Graphics.DrawRect(fishingAchieveStart, height - 40.0f, 10.0f, 25.0f, 0, 255, 0, 255, 0);

                // Draw the moving bar
                Graphics.DrawRect(fishingBarPosition, height - 41.0f, 2.0f, 26.0f, 255, 255, 255, 255, 0);

                if (movementRight)
                {
                    // Move the bar to the right
                    fishingBarPosition += 1.0f;

                    if (fishingBarPosition > fishingBarMax)
                    {
                        fishingBarPosition = fishingBarMax;
                        movementRight = false;
                    }
                }
                else
                {
                    // Move the bar to the left
                    fishingBarPosition -= 1.0f;

                    if (fishingBarPosition < fishingBarMin)
                    {
                        fishingBarPosition = fishingBarMin;
                        movementRight = true;
                    }
                }
            }
        }
    }
}
