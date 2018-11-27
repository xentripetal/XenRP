using System.Collections.Generic;
using WiredPlayers_Client.model;

namespace WiredPlayers_Client.globals
{
    class Constants
    {
        public static readonly int SEX_MALE = 0;
        public static readonly int SEX_FEMALE = 1;

        public static readonly List<ClothesType> CLOTHES_TYPES = new List<ClothesType>()
        {
            new ClothesType(0, 1, "clothes.masks"), new ClothesType(0, 3, "clothes.torso"), new ClothesType(0, 4, "clothes.legs"),
            new ClothesType(0, 5, "clothes.bags"), new ClothesType(0, 6, "clothes.feet"), new ClothesType(0, 7, "clothes.complements"),
            new ClothesType(0, 8, "clothes.undershirt"), new ClothesType(0, 11, "clothes.tops"), new ClothesType(1, 0, "clothes.hats"),
            new ClothesType(1, 1, "clothes.glasses"), new ClothesType(1, 2, "clothes.earrings"), new ClothesType(1, 6, "clothes.watches"),
            new ClothesType(1, 7, "clothes.bracelets")
        };

        public static readonly List<string> TATTOO_ZONES = new List<string>()
        {
            "tattoo.torso", "tattoo.head", "tattoo.left-arm", "tattoo.right-arm", "tattoo.left-leg", "tattoo.right-leg"
        };

        public static readonly List<FaceOption> MALE_FACE_OPTIONS = new List<FaceOption>()
        {
            new FaceOption("hairdresser.hair", 0, 36), new FaceOption("hairdresser.hair-primary", 0, 63), new FaceOption("hairdresser.hair-secondary", 0, 63),
            new FaceOption("hairdresser.eyebrows", 0, 33), new FaceOption("hairdresser.eyebrows-color", 0, 63), new FaceOption("hairdresser.beard", -1, 36),
            new FaceOption("hairdresser.beard-color", 0, 63)
        };

        public static readonly List<FaceOption> FEMALE_FACE_OPTIONS = new List<FaceOption>()
        {
            new FaceOption("hairdresser.hair", 0, 38), new FaceOption("hairdresser.hair-primary", 0, 63), new FaceOption("hairdresser.hair-secondary", 0, 63),
            new FaceOption("hairdresser.eyebrows", 0, 33), new FaceOption("hairdresser.eyebrows-color", 0, 63)
        };
    }
}
