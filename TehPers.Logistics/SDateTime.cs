﻿using System;
using StardewValley;
using TehPers.CoreMod.Api.Static.Enums;
using TehPers.CoreMod.Api.Static.Extensions;

namespace TehPers.Logistics {
    public readonly struct SDateTime : IComparable<SDateTime> {

        /// <summary>The total number of elapsed years.</summary>
        public float TotalYears => (float) this.TotalMinutes / (4f * 28f * 2400f);

        /// <summary>The total number of elapsed seasons.</summary>
        public float TotalSeasons => (float) this.TotalMinutes / (28f * 2400f);

        /// <summary>The total number of elapsed days.</summary>
        public float TotalDays => (float) this.TotalMinutes / 2400f;

        /// <summary>The total number of elapsed minutes.</summary>
        public int TotalMinutes { get; }

        /// <summary>The number of elapsed years.</summary>
        public int Year => (int) this.TotalYears + 1;

        /// <summary>The number of elapsed seasons in the year.</summary>
        public Season Season {
            get {
                switch ((int) this.TotalSeasons % 4) {
                    case 1:
                        return Season.Summer;
                    case 2:
                        return Season.Fall;
                    case 3:
                        return Season.Winter;
                    default:
                        return Season.Spring;
                }
            }
        }

        /// <summary>The number of elapsed days in the season.</summary>
        public int DayOfSeason => (int) this.TotalDays % 28 + 1;

        /// <summary>The current time of day in SDV format (hhmm).</summary>
        public int TimeOfDay => this.TotalMinutes + 40 * (this.TotalMinutes / 60);

        /// <summary>The number of elapsed minutes in the day. This is not in SDV time format, use <see cref="TimeOfDay"/> instead if that is needed.</summary>
        public int MinutesOfDay => this.TotalMinutes % 2400;

        public SDateTime(int years, Season season, int days = 0, int minutes = 0) {
            int seasons;
            switch (season) {
                case Season.Summer:
                    seasons = 1;
                    break;
                case Season.Fall:
                    seasons = 2;
                    break;
                case Season.Winter:
                    seasons = 3;
                    break;
                default:
                    seasons = 0;
                    break;
            }

            this.TotalMinutes = minutes + days * 2400 + seasons * 28 * 2400 + years * 4 * 28 * 2400;
        }

        public SDateTime(int years, int seasons = 0, int days = 0, int minutes = 0) {
            this.TotalMinutes = minutes + days * 2400 + seasons * 28 * 2400 + years * 4 * 28 * 2400;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            return obj is SDateTime other && this.CompareTo(other) == 0;
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return this.TotalMinutes;
        }

        /// <inheritdoc />
        public int CompareTo(SDateTime other) {
            return this.TotalMinutes.CompareTo(other.TotalMinutes);
        }

        public override string ToString() {
            int timeOfDay = this.TimeOfDay;
            return $"{this.Season} {this.DayOfSeason}, {this.Year} {timeOfDay / 100}:{timeOfDay % 100}";
        }

        public static SDateTime operator +(SDateTime first, STimeSpan second) => new SDateTime(0, 0, 0, first.TotalMinutes + second.TotalMinutes);
        public static SDateTime operator -(SDateTime first, STimeSpan second) => new SDateTime(0, 0, 0, first.TotalMinutes - second.TotalMinutes);
        public static SDateTime operator -(SDateTime first, SDateTime second) => new SDateTime(0, 0, 0, first.TotalMinutes - second.TotalMinutes);
        public static bool operator >(SDateTime first, SDateTime second) => first.CompareTo(second) > 0;
        public static bool operator >=(SDateTime first, SDateTime second) => first.CompareTo(second) >= 0;
        public static bool operator <(SDateTime first, SDateTime second) => first.CompareTo(second) < 0;
        public static bool operator <=(SDateTime first, SDateTime second) => first.CompareTo(second) <= 0;
        public static bool operator ==(SDateTime first, SDateTime second) => first.Equals(second);
        public static bool operator !=(SDateTime first, SDateTime second) => !first.Equals(second);

        public static SDateTime FromDateAndTime(int year, Season season, int dayOfSeason, int timeOfDay) {
            return new SDateTime(year, season, dayOfSeason, 60 * (timeOfDay / 100) + timeOfDay % 100);
        }

        /// <summary>The current date and time.</summary>
        public static SDateTime Now => SDateTime.FromDateAndTime(Game1.year, Game1.currentSeason.GetSeason() ?? Season.Spring, Game1.dayOfMonth, Game1.timeOfDay);

        /// <summary>The current date.</summary>
        public static SDateTime Today => new SDateTime(Game1.year, Game1.currentSeason.GetSeason() ?? Season.Spring, Game1.dayOfMonth);
    }
}