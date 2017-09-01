using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Stardew.Framework {
    [Flags]
    public enum WaterType : int {
        /** <summary>Game ID is 1</summary> **/
        LAKE = 1,

        /** <summary>Game ID is 0</summary> **/
        RIVER = 2,

        /** <summary>Game ID is -1</summary> **/
        BOTH = WaterType.LAKE | WaterType.RIVER
    }

    [Flags]
    public enum Weather : int {
        SUNNY = 1,
        RAINY = 2,
        BOTH = Weather.SUNNY | Weather.RAINY
    }

    [Flags]
    public enum Season : int {
        SPRING = 1,
        SUMMER = 2,
        FALL = 4,
        WINTER = 8,

        SPRINGSUMMER = Season.SPRING | Season.SUMMER,
        SPRINGFALL = Season.SPRING | Season.FALL,
        SUMMERFALL = Season.SUMMER | Season.FALL,
        SPRINGSUMMERFALL = Season.SPRING | Season.SUMMER | Season.FALL,
        SPRINGWINTER = Season.SPRING | Season.WINTER,
        SUMMERWINTER = Season.SUMMER | Season.WINTER,
        SPRINGSUMMERWINTER = Season.SPRING | Season.SUMMER | Season.WINTER,
        FALLWINTER = Season.FALL | Season.WINTER,
        SPRINGFALLWINTER = Season.SPRING | Season.FALL | Season.WINTER,
        SUMMERFALLWINTER = Season.SUMMER | Season.FALL | Season.WINTER,
        SPRINGSUMMERFALLWINTER = Season.SPRING | Season.SUMMER | Season.FALL | Season.WINTER
    }
}