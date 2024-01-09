// <copyright file="WorkShift.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.CustomAI
{
    /// <summary>
    /// An enumeration that describes the citizen's work shift.
    /// </summary>
    internal enum SchoolClass : byte
    {
        /// <summary>The citizen will not go to school.</summary>
        NoClass,

        /// <summary>The citizen will study during the day.</summary>
        DayClass,

        /// <summary>The citizen will study during the night.</summary>
        NightClass,

    }
}
