// Aleksey Pustohaylov
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Obs.Gunswords.dev.Common.Resources.Pub.Out.Substances.Interfaces;
using Obs.Gunswords.dev.Common.Resources.Pub.Out.Substances.Types;
using Obs.Gunswords.dev.Common.Types;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Localization.Pub.Substances.Interfaces;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Localization.Pub.Types;

namespace Obs.Gunswords.dev.Presentation.Essence.Simple.Localization.Pub.Substances.Types
{
    public sealed class ResourceBook : IResourceBook
    {
        #region Data
        //===============================================================================================[]
        private readonly IList<IResourceDescribe> _resourceDescribe = new List<IResourceDescribe>();
        private readonly Dictionary<ResourceInfo, IResourceDescribe> _dictionaryResourceDescribe =
            new Dictionary<ResourceInfo, IResourceDescribe>();
        //===============================================================================================[]
        #endregion




        #region IResourceLocationBook
        //===============================================================================================[]
        public void LoadFromXml( string file )
        {
            ClearDescribedResourceLocationList();
            LoadDescriptionData( file );
        }

        //-------------------------------------------------------------------------------------[]
        public IEnumerable<IResourceDescribe> GetResourceDescribe()
        {
            return _resourceDescribe;
        }

        //-------------------------------------------------------------------------------------[]
        public Dictionary<ResourceInfo, IResourceDescribe> GetResourceDictionaryDescribe()
        {
            return _dictionaryResourceDescribe;
        }

        //===============================================================================================[]
        #endregion




        #region Routines
        //===============================================================================================[]
        private void LoadDescriptionData( string data )
        {
            var descriptionData = GetDescriptionData( data );
            LoadDescriptions( descriptionData );
        }

        //-------------------------------------------------------------------------------------[]
        private void LoadDescriptions( LocalizationDescriptionData descriptionData )
        {
            descriptionData.Records.ForEach( LoadDescription );
        }

        //-------------------------------------------------------------------------------------[]
        private void LoadDescription( LocalizationDescriptionData.Record record )
        {
            var location = GetResourceLocation( record.Location );
            var text = record.Text;
            var description = GetLocalizationResourceDescription( record );
            CreateDescribedLocation( description, location, text );
        }

        //-------------------------------------------------------------------------------------[]
        private void CreateDescribedLocation(
            ILocalizationResourceDescription description,
            IResourceLocation location,
            string text )
        {
            AddDescribedLocation( GetDescribedResourceLocation( description, location, text ) );
        }

        //-------------------------------------------------------------------------------------[]
        private static LocalizationDescriptionData GetDescriptionData( string data )
        {
            return DoGetObject<LocalizationDescriptionData>( data );
        }

        //-------------------------------------------------------------------------------------[]
        private static T DoGetObject< T >( string data )
        {
            var serializer = new XmlSerializer( typeof( T ) );
            return ( T ) serializer.Deserialize( GetXmlReader( data ) );
        }

        //-------------------------------------------------------------------------------------[]
        private static XmlReader GetXmlReader( string data )
        {
            return XmlReader.Create( new StringReader( GetPreparedText( data ) ) );
        }

        //-------------------------------------------------------------------------------------[]
        private static string GetPreparedText( string data )
        {
            return Regex.Replace( data, @"<(.*)xmlns=(.*)>", @"<$1xmlns:ignore=$2>" );
        }

        //-------------------------------------------------------------------------------------[]
        private static IResourceLocation GetResourceLocation( string location )
        {
            return new ResourceLocation( location );
        }

        //-------------------------------------------------------------------------------------[]
        private static ILocalizationResourceDescription GetLocalizationResourceDescription(
            LocalizationDescriptionData.Record descriptionResource )
        {
            return new LocalizationResourceDescription(
                descriptionResource.LanguageProperty, descriptionResource.ValueProperty );
        }

        //-------------------------------------------------------------------------------------[]
        private static IResourceDescribe GetDescribedResourceLocation(
            ILocalizationResourceDescription description,
            IResourceLocation location,
            string text )
        {
            return new ResourceDescribe( description, location, text );
        }

        //-------------------------------------------------------------------------------------[]
        private void AddDescribedLocation( IResourceDescribe describedResourceLocation )
        {
            _dictionaryResourceDescribe.Add(
                new ResourceInfo {
                    LanguageProperty = describedResourceLocation.Description.LanguageProperty.ToLower(),
                    ValueProperty = describedResourceLocation.Description.ValueProperty.ToLower()
                },
                describedResourceLocation );
        }

        //-------------------------------------------------------------------------------------[]
        private void ClearDescribedResourceLocationList()
        {
            _resourceDescribe.Clear();
            _dictionaryResourceDescribe.Clear();
        }

        //===============================================================================================[]
        #endregion
    }
}