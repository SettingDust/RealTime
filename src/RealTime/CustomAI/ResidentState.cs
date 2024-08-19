// ResidentState.cs

namespace RealTime.CustomAI
{
    /// <summary>
    /// Possible citizen's target states. While moving to the target building, citizen will already have the target state.
    /// </summary>
    internal enum ResidentState : byte
    {
        /// <summary>The state is not defined. A good time to make a decision.</summary>
        Unknown,

        /// <summary>The citizen should be ignored, just dummy traffic.</summary>
        Ignored,

        /// <summary>The citizen is in the home building.</summary>
        AtHome,

        /// <summary>The citizen is in the school building.</summary>
        AtSchool,

        /// <summary>The citizen is in the work building.</summary>
        AtWork,

        /// <summary>The citizen is shopping in a commercial building.</summary>
        Shopping,

        /// <summary>The citizen is having lunch time in a commercial building or university cafeteria.</summary>
        Lunch,

        /// <summary>The citizen is in a leisure building or in a beautification building.</summary>
        Relaxing,

        /// <summary>The citizen visits a building.</summary>
        Visiting,

        /// <summary>The citizen has to evacuate the current building (or area).</summary>
        Evacuation,

        /// <summary>The citizen is in a shelter building.</summary>
        InShelter,

        /// <summary>The citizen was in transition from one state to another.</summary>
        InTransition,
    }
}
