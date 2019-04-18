﻿using RAGE;
using RAGE.Elements;
using System.Collections.Generic;
using WiredPlayers_Client.globals;
using WiredPlayers_Client.model;
using Newtonsoft.Json;
using System;

namespace WiredPlayers_Client.business
{
    class Hairdresser : Events.Script
    {
        List<int> facialHair = null;
        FacialHair initialHair = null;

        public Hairdresser()
        {
            Events.Add("showHairdresserMenu", ShowHairdresserMenuEvent);
            Events.Add("updateFacialHair", UpdateFacialHairEvent);
            Events.Add("applyHairdresserChanges", ApplyHairdresserChangesEvent);
            Events.Add("cancelHairdresserChanges", CancelHairdresserChangesEvent);
        }

        private void ShowHairdresserMenuEvent(object[] args)
        {
            // Get the variables from the arguments
            int sex = Convert.ToInt32(args[0]);
            string skinJson = args[1].ToString();
            string businessName = args[2].ToString();

            // Add the options
            string faceOption = JsonConvert.SerializeObject(sex == Constants.SEX_MALE ? Constants.MALE_FACE_OPTIONS : Constants.FEMALE_FACE_OPTIONS);

            // Initialize the face values
            initialHair = JsonConvert.DeserializeObject<FacialHair>(skinJson);

            facialHair = new List<int>();
            facialHair.Add(initialHair.hairModel);
            facialHair.Add(initialHair.firstHairColor);
            facialHair.Add(initialHair.secondHairColor);
            facialHair.Add(initialHair.eyebrowsModel);
            facialHair.Add(initialHair.eyebrowsColor);
            facialHair.Add(initialHair.beardModel);
            facialHair.Add(initialHair.beardColor);

            // Create hairdressers' menu
            Browser.CreateBrowserEvent(new object[] { "package://statics/html/sideMenu.html", "populateHairdresserMenu", faceOption, JsonConvert.SerializeObject(facialHair), businessName });
        }

        private void UpdateFacialHairEvent(object[] args)
        {
            // Get the variables from the arguments
            int slot = Convert.ToInt32(args[0]);
            int value = Convert.ToInt32(args[1]);

            // Save the new value
            facialHair[slot] = value;

            // Check if the beard is out of range
            int beardModel = facialHair[5] < 0 ? 255 : facialHair[5];

            // Update the player's head
            Player.LocalPlayer.SetComponentVariation(2, facialHair[0], 0, 0);
            Player.LocalPlayer.SetHairColor(facialHair[1], facialHair[2]);
            Player.LocalPlayer.SetHeadOverlay(1, beardModel, 1.0f);
            Player.LocalPlayer.SetHeadOverlay(2, facialHair[3], 1.0f);
            Player.LocalPlayer.SetHeadOverlayColor(1, 1, facialHair[6], 0);
            Player.LocalPlayer.SetHeadOverlayColor(2, 1, facialHair[4], 0);
        }

        private void ApplyHairdresserChangesEvent(object[] args)
        {
            FacialHair generatedFace = new FacialHair();
            generatedFace.hairModel = facialHair[0];
            generatedFace.firstHairColor = facialHair[1];
            generatedFace.secondHairColor = facialHair[2];
            generatedFace.eyebrowsModel = facialHair[3];
            generatedFace.eyebrowsColor = facialHair[4];
            generatedFace.beardModel = facialHair[5] < 0 ? 255 : facialHair[5];
            generatedFace.beardColor = facialHair[6];

            Events.CallRemote("changeHairStyle", JsonConvert.SerializeObject(generatedFace));
        }

        private void CancelHairdresserChangesEvent(object[] args)
        {
            // Revert the changes
            Player.LocalPlayer.SetComponentVariation(2, initialHair.hairModel, 0, 0);
            Player.LocalPlayer.SetHairColor(initialHair.firstHairColor, initialHair.secondHairColor);
            Player.LocalPlayer.SetHeadOverlay(1, initialHair.beardModel, 1.0f);
            Player.LocalPlayer.SetHeadOverlay(2, initialHair.eyebrowsModel, 1.0f);
            Player.LocalPlayer.SetHeadOverlayColor(1, 1, initialHair.beardColor, 0);
            Player.LocalPlayer.SetHeadOverlayColor(2, 1, initialHair.eyebrowsColor, 0);
        }
    }
}
