﻿using System;
using System.Reflection;

namespace TehPers.Stardew.Framework {

    internal class Helpers {
        public static T copyFields<T>(T original, T target) {
            Type typ = original.GetType();
            while (typ != null) {
                FieldInfo[] fields = typ.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                    field.SetValue(target, field.GetValue(original));
                typ = typ.BaseType;
            }

            return target;
        }

        public static Season? toSeason(string s) {
            switch (s.ToLower()) {
                case "spring":
                    return Season.SPRING;
                case "summer":
                    return Season.SUMMER;
                case "fall":
                    return Season.FALL;
                case "winter":
                    return Season.WINTER;
                default:
                    return null;
            }
        }

        public static Weather toWeather(bool raining) {
            return raining ? Weather.RAINY : Weather.SUNNY;
        }

        public static WaterType? convertWaterType(int type) {
            switch (type) {
                case -1:
                    return WaterType.BOTH;
                case 0:
                    return WaterType.RIVER;
                case 1:
                    return WaterType.LAKE;
                default:
                    return null;
            }
        }

        public static int convertWaterType(WaterType type) {
            return type == WaterType.BOTH ? -1 : (type == WaterType.RIVER ? 0 : 1);
        }
    }
}
