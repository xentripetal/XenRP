using GTANetworkAPI;
using System;
using System.Collections.Generic;
using WiredPlayers.globals;

namespace WiredPlayers.model
{
    public class SkinModel : Script
    {
        public int firstHeadShape { get; set; }
        public int secondHeadShape { get; set; }

        public int firstSkinTone { get; set; }
        public int secondSkinTone { get; set; }

        public float headMix { get; set; }
        public float skinMix { get; set; }

        public int hairModel { get; set; }
        public int firstHairColor { get; set; }
        public int secondHairColor { get; set; }

        public int beardModel { get; set; }
        public int beardColor { get; set; }

        public int chestModel { get; set; }
        public int chestColor { get; set; }

        public int blemishesModel { get; set; }
        public int ageingModel { get; set; }
        public int complexionModel { get; set; }
        public int sundamageModel { get; set; }
        public int frecklesModel { get; set; }

        public int eyesColor { get; set; }
        public int eyebrowsModel { get; set; }
        public int eyebrowsColor { get; set; }

        public int makeupModel { get; set; }
        public int blushModel { get; set; }
        public int blushColor { get; set; }
        public int lipstickModel { get; set; }
        public int lipstickColor { get; set; }

        public float noseWidth { get; set; }
        public float noseHeight { get; set; }
        public float noseLength { get; set; }
        public float noseBridge { get; set; }
        public float noseTip { get; set; }
        public float noseShift { get; set; }
        public float browHeight { get; set; }
        public float browWidth { get; set; }
        public float cheekboneHeight { get; set; }
        public float cheekboneWidth { get; set; }
        public float cheeksWidth { get; set; }
        public float eyes { get; set; }
        public float lips { get; set; }
        public float jawWidth { get; set; }
        public float jawHeight { get; set; }
        public float chinLength { get; set; }
        public float chinPosition { get; set; }
        public float chinWidth { get; set; }
        public float chinShape { get; set; }
        public float neckWidth { get; set; }

        public void ApplyPlayerCustomization(Client player, int sex)
        {
            // Populate the head
            HeadBlend headBlend = new HeadBlend();
            headBlend.ShapeFirst = Convert.ToByte(firstHeadShape);
            headBlend.ShapeSecond = Convert.ToByte(secondHeadShape);
            headBlend.SkinFirst = Convert.ToByte(firstSkinTone);
            headBlend.SkinSecond = Convert.ToByte(secondSkinTone);
            headBlend.ShapeMix = headMix;
            headBlend.SkinMix = skinMix;

            // Get the hair and eyes colors
            byte eyeColor = Convert.ToByte(eyesColor);
            byte hairColor = Convert.ToByte(firstHairColor);
            byte hightlightColor = Convert.ToByte(secondHairColor);

            // Add the face features
            float[] faceFeatures = new float[]
            {
                noseWidth, noseHeight, noseLength, noseBridge, noseTip, noseShift, browHeight,
                browWidth, cheekboneHeight, cheekboneWidth, cheeksWidth, eyes, lips, jawWidth,
                jawHeight, chinLength, chinPosition, chinWidth, chinShape, neckWidth
            };

            // Populate the head overlays
            Dictionary<int, HeadOverlay> headOverlays = new Dictionary<int, HeadOverlay>();

            for (int i = 0; i < Constants.MAX_HEAD_OVERLAYS; i++)
            {
                // Get the overlay model and color
                int[] overlayData = GetOverlayData(i);

                // Create the overlay
                HeadOverlay headOverlay = new HeadOverlay();
                headOverlay.Index = Convert.ToByte(overlayData[0]);
                headOverlay.Color = Convert.ToByte(overlayData[1]);
                headOverlay.SecondaryColor = 0;
                headOverlay.Opacity = 1.0f;

                // Add the overlay
                headOverlays[i] = headOverlay;
            }

            // Update the character's skin
            player.SetCustomization(sex == Constants.SEX_MALE, headBlend, eyeColor, hairColor, hightlightColor, faceFeatures, headOverlays, new Decoration[] { });
            player.SetClothes(2, hairModel, 0);
        }

        private int[] GetOverlayData(int index)
        {
            int[] overlayData = new int[2];

            switch (index)
            {
                case 0:
                    overlayData[0] = blemishesModel;
                    overlayData[1] = 0;
                    break;
                case 1:
                    overlayData[0] = beardModel;
                    overlayData[1] = beardColor;
                    break;
                case 2:
                    overlayData[0] = eyebrowsModel;
                    overlayData[1] = eyebrowsColor;
                    break;
                case 3:
                    overlayData[0] = ageingModel;
                    overlayData[1] = 0;
                    break;
                case 4:
                    overlayData[0] = makeupModel;
                    overlayData[1] = 0;
                    break;
                case 5:
                    overlayData[0] = blushModel;
                    overlayData[1] = blushColor;
                    break;
                case 6:
                    overlayData[0] = complexionModel;
                    overlayData[1] = 0;
                    break;
                case 7:
                    overlayData[0] = sundamageModel;
                    overlayData[1] = 0;
                    break;
                case 8:
                    overlayData[0] = lipstickModel;
                    overlayData[1] = lipstickColor;
                    break;
                case 9:
                    overlayData[0] = frecklesModel;
                    overlayData[1] = 0;
                    break;
                case 10:
                    overlayData[0] = chestModel;
                    overlayData[1] = chestColor;
                    break;
            }

            return overlayData;
        }
    }
}
