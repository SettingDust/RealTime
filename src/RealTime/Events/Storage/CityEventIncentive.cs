// CityEventIncentive.cs

namespace RealTime.Events.Storage
{
    using System.Xml.Serialization;

    /// <summary>
    /// A storage class for the city event incentive settings.
    /// </summary>
    public class CityEventIncentive
    {
        [XmlAttribute("Name")]
        public string _name = "";

        [XmlAttribute("Cost")]
        public float _cost = 3;

        [XmlAttribute("ReturnCost")]
        public float _returnCost = 10;

        [XmlAttribute("ActiveWhenRandomEvent")]
        public bool _activeWhenRandomEvent = false;

        [XmlElement("Description", IsNullable = false)]
        public string _description = "";

        [XmlElement("PositiveEffect", IsNullable = false)]
        public int _positiveEffect = 10;

        [XmlElement("NegativeEffect", IsNullable = false)]
        public int _negativeEffect = 10;
    }
}
