// Aleksey Pustohaylov
using System;
using System.Collections.Generic;
using System.Linq;
using Obs.Gunswords.dev.Common.Types;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Imp.Stuff;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Imp.Stuff.TimerPrograms;

namespace Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Imp.Workings
{
    internal partial class Scenarist
    {
        #region Pieces
        //===============================================================================================[] 
        private readonly Dictionary<string, string> _oldNewCodesAccord = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _rolesAccord = new Dictionary<string, string>();
        private string _baseCodeAlias;
        //-------------------------------------------------------------------------------------[]
        private void AddPiece(
            DraftScenario draft,
            string piece )
        {
            ResetOldNewCodesDict();
            var textlines = SplitLines( piece );
            textlines.ForEach( AddScenarioLineToDraft, draft );
        }

        //-------------------------------------------------------------------------------------[]
        private void ResetOldNewCodesDict()
        {
            _oldNewCodesAccord.Clear();
            if( !string.IsNullOrEmpty( _baseCodeAlias ) )
                _oldNewCodesAccord.Add( Constants.BaseRecordCode, _baseCodeAlias );
        }

        //-------------------------------------------------------------------------------------[]
        private void SetLastCodeAsBase( DraftScenario draft )
        {
            var lastCode = draft.GetLastCode();
            if( lastCode != Constants.BaseRecordCode )
                lastCode += ".end";
            _baseCodeAlias = lastCode;
        }

        //-------------------------------------------------------------------------------------[]
        private void AddScenarioLineToDraft(
            DraftScenario draft,
            string line )
        {
            var code = GetNewCode( draft, line );
            var role = GetRoleWithAction( line );
            var newBaseCode = GetNewBaseCode( line );

            if( IsParsingFailed( code, role, newBaseCode ) )
                return;

            draft.AddPreparedScenarioLine( code, newBaseCode, role );
        }

        //-------------------------------------------------------------------------------------[]
        private string GetNewCode(
            DraftScenario draft,
            string line )
        {
            var oldCode = ParseCode( line );
            if( oldCode == Constants.BaseRecordCode )
                return Constants.ParsingError;
            var newCode = GenerateNewCode( draft );
            _oldNewCodesAccord[ oldCode ] = newCode;
            return newCode;
        }

        //-------------------------------------------------------------------------------------[]
        private string GetNewBaseCode( string line )
        {
            var baseCode = ParseBaseCode( line );
            var oldBaseOnlyCode = ParseBaseOnlyCode( line );
            return IsParsingFailed( baseCode, oldBaseOnlyCode )
                       ? Constants.ParsingError
                       : baseCode.Replace( oldBaseOnlyCode, _oldNewCodesAccord[ oldBaseOnlyCode ] );
        }

        //-------------------------------------------------------------------------------------[]
        private string GetRoleWithAction( string line )
        {
            var start = line.IndexOf( Constants.TimeRecordCloseSymbol ) + 1;
            if( IsIndexOfFailed( start ) )
                return Constants.ParsingError;
            var roleWithAction = line.Substring( start, line.Length - start ).Trim();
            var oldRole = ParseRole( roleWithAction );
            return _rolesAccord.ContainsKey( oldRole )
                       ? _rolesAccord[ oldRole ] + roleWithAction.Substring( oldRole.Length )
                       : roleWithAction;
        }

        //===============================================================================================[]
        #endregion




        #region Other Methods
        //===============================================================================================[]
        private static IEnumerable<string> SplitLines( string text )
        {
            return text.Split( Constants.LineDelimiter ).Select( TrimLine ).Where( LineIsNotEmpty );
        }

        //-------------------------------------------------------------------------------------[]
        private static string TrimLine( string line )
        {
            return line.Trim();
        }

        //-------------------------------------------------------------------------------------[]
        private static bool LineIsNotEmpty( string line )
        {
            return !string.IsNullOrEmpty( line );
        }

        //-------------------------------------------------------------------------------------[]
        private static string GenerateNewCode( DraftScenario draft )
        {
            return "C" + draft.ScenarioLinesCount;
        }

        //===============================================================================================[]
        #endregion




        #region Parsing
        //===============================================================================================[]
        private static string ParseCode( string line )
        {
            var finish = line.IndexOf( Constants.RecordCodeDelimiter );
            return IsIndexOfFailed( finish )
                       ? Constants.ParsingError
                       : line.Substring( 0, finish );
        }

        //-------------------------------------------------------------------------------------[]
        private static string ParseBaseCode( string line )
        {
            var start = line.IndexOf( Constants.TimeRecordOpenSymbol );
            var finish = line.IndexOf( Constants.TimeRecordCloseSymbol );
            return IsIndexOfFailed( start, finish )
                       ? Constants.ParsingError
                       : line.Substring( start + 1, finish - start - 1 );
        }

        //-------------------------------------------------------------------------------------[]
        private static string ParseBaseOnlyCode( string line )
        {
            var baseCode = ParseBaseCode( line );
            if( baseCode == Constants.ParsingError )
                return Constants.ParsingError;
            var separators = new[] {'.', '+', '-'};
            return baseCode.Split( separators ).First();
        }

        //-------------------------------------------------------------------------------------[]
        private static string ParseRole( string line )
        {
            return line.Split( new[] {'.'} ).First();
        }

        //===============================================================================================[]
        #endregion




        #region Verifications
        //===============================================================================================[]     
        private static bool IsParsingFailed( params string[] list )
        {
            return list.Any( line => line == Constants.ParsingError );
        }

        //-------------------------------------------------------------------------------------[]
        private static bool IsIndexOfFailed( params int[] list )
        {
            return list.Any( index => index == Constants.IndexOfFailed );
        }

        //===============================================================================================[]
        #endregion
    }
}