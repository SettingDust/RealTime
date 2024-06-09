// CityEventCosts.cs

namespace RealTime.Events.Storage
{
    using System.Xml.Serialization;

    /// <summary>
    /// A storage class for the city event costs settings.
    /// </summary>
    public class CityEventCosts
    {
        /// <summary>Creation of event price.</summary>
        [XmlElement("Creation", IsNullable = false)]
        public float _creation = 100;

        /// <summary>How much paid per head.</summary>
        [XmlElement("PerHead", IsNullable = false)]
        public float _perHead = 5;

        /// <summary>Price for advertising signs.</summary>
        [XmlElement("AdvertisingSigns", IsNullable = false)]
        public float _advertisingSigns = 20000;

        /// <summary>Price for advertising on TV.</summary>
        [XmlElement("AdvertisingTV", IsNullable = false)]
        public float _advertisingTV = 5000;

        /// <summary>Gets or sets the ticket price for this event.</summary>
        [XmlElement("EntryCost", IsNullable = false)]
        public float _entry = 10;
    }
}
