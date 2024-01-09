// <copyright file="WorkStatus.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.CustomAI
{
    /// <summary>
    /// Describes the school/university status of a citizen.
    /// </summary>
    internal enum SchoolStatus : byte
    {
        /// <summary>No special handling.</summary>
        None,

        /// <summary>The citizen is in school/university.</summary>
        Studying,

        /// <summary>The citizen is on vacation.</summary>
        OnVacation,
    }
}
