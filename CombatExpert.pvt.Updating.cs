// Aleksey Pustohaylov
using System;
using System.Collections.Generic;
using System.Linq;
using Obs.Gunswords.dev.Common.DataTransferProtocol;
using Obs.Gunswords.dev.Common.Diagnostic;
using Obs.Gunswords.dev.Common.Types;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Combat.Imp.Stuff;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Combat.Pub.Types;

namespace Obs.Gunswords.dev.Presentation.Essence.Simple.Combat.Imp.Workers
{
    internal sealed partial class CombatExpert
    {
        #region Updating
        //===============================================================================================[]
        private float _stoppedTimeDuration;
        private float _durationOfCurrentPause;
        private float _pauseStartTime;
        private float _pauseFinishTime;

        private float StoppedTimeDuration
        {
            get { return _stoppedTimeDuration + _durationOfCurrentPause; }
        }

        private DateTime _startCombatTime = DateTime.MinValue;
        private DateTime _endCombatTime = DateTime.MinValue;
        //-------------------------------------------------------------------------------------[]
        private FighterId _currentFighter = new FighterId();
        private FighterId _prevFighter = new FighterId();
        //-------------------------------------------------------------------------------------[]
        private void SetUpdating()
        {
            Requirements.OnUpdate += DoUpdate;
            Requirements.OnModelReceive += TryDoModelReceive;
            Requirements.OnChangesReceive += TryDoChangesReceive;
        }

        //-------------------------------------------------------------------------------------[]
        private void DoUpdate( float secondsSinceStartup )
        {
            UpdateCurrentAndPrevFighters();
            UpdateStoppedTimeDuration( secondsSinceStartup );
            SetCurrentTime( secondsSinceStartup );
        }

        //-------------------------------------------------------------------------------------[]
        private void UpdateCurrentAndPrevFighters()
        {
            _prevFighter = _currentFighter;
            _currentFighter = _combatModel.ActiveFighter;
        }

        //-------------------------------------------------------------------------------------[]
        private void UpdateStoppedTimeDuration( float secondsSinceStartup )
        {
            if( FighterWasChanged() )
                ResetPauseParams();

            if( GameIsPaused ) {
                SetPauseStartTimeIfNeed( secondsSinceStartup );
                ResetPauseFinishTime();

                CalculateDurationOfCurrentPause( secondsSinceStartup );
            }
            else {
                SetPauseFinishTimeIfNeed( secondsSinceStartup );
                IncreaseStoppedTimeDurationIfNeed();
                ResetPauseStartTime();

                ResetDurationOfCurrentPause();
            }
        }

        //-------------------------------------------------------------------------------------[]
        private bool FighterWasChanged()
        {
            return _prevFighter != _currentFighter;
        }

        //-------------------------------------------------------------------------------------[]
        private void ResetDurationOfCurrentPause()
        {
            _durationOfCurrentPause = 0;
        }

        //-------------------------------------------------------------------------------------[]
        private void CalculateDurationOfCurrentPause( float currentTime )
        {
            _durationOfCurrentPause = currentTime - _pauseStartTime;
        }

        //-------------------------------------------------------------------------------------[]
        private void IncreaseStoppedTimeDurationIfNeed()
        {
            if( _pauseStartTime != 0 )
                _stoppedTimeDuration += _pauseFinishTime - _pauseStartTime;
        }

        //-------------------------------------------------------------------------------------[]
        private void SetPauseStartTimeIfNeed( float value )
        {
            if( _pauseStartTime == 0 )
                _pauseStartTime = value;
        }

        //-------------------------------------------------------------------------------------[]
        private void SetPauseFinishTimeIfNeed( float value )
        {
            if( _pauseFinishTime == 0 )
                _pauseFinishTime = value;
        }

        //-------------------------------------------------------------------------------------[]
        private void ResetPauseStartTime()
        {
            _pauseStartTime = 0;
        }

        //-------------------------------------------------------------------------------------[]
        private void ResetPauseFinishTime()
        {
            _pauseFinishTime = 0;
        }

        //-------------------------------------------------------------------------------------[]
        private void DoModelReceive( ClientModel model )
        {
            AssertModelIsValid( model );

            UpdateModel( model );
            SetModelUpdateTime();
            ResetPauseParams();
        }

        //-------------------------------------------------------------------------------------[]
        private void ResetPauseParams()
        {
            ResetPauseFinishTime();
            ResetPauseStartTime();
            _stoppedTimeDuration = 0;
            _durationOfCurrentPause = 0;
        }

        //-------------------------------------------------------------------------------------[]
        private void TryDoModelReceive( ClientModel model )
        {
            try {
                DoModelReceive( model );
            }
            catch( GunswordsException ex ) {
                ResetGame();
            }
        }

        //-------------------------------------------------------------------------------------[]
        private void UpdateModel( ClientModel model )
        {
            SetNewFullModel( CombatModelBuilder.BuildModel( model ) );
        }

        //-------------------------------------------------------------------------------------[]
        private void PlayerReady()
        {
            Requirements.PlayerReady();
            _isFirstFullCombatModelReceived = true;
        }

        //-------------------------------------------------------------------------------------[]
        private void DoUpdateEquipments( EquipmentInfo equipments )
        {
            UpdateWeaponInfo( CombatModelBuilder.CreateWeaponInfos( equipments ) );
        }

        //-------------------------------------------------------------------------------------[]
        private void DoChangesReceive( ClientChangesModel model )
        {
            AssertModelIsValid( model );
            
            model.Changes.ForEach( UpdateCombatModel );
        }

        //-------------------------------------------------------------------------------------[]
        private void TryDoChangesReceive( ClientChangesModel model )
        {
            try {
                DoChangesReceive( model );
            }
            catch( GunswordsException ex ) {
                ResetGame();
            }
        }

        //-------------------------------------------------------------------------------------[]
        private void UpdateCombatModel( ClientChangesModel.Changeset changeset )
        {
            AssertChangesetVersionIsCorrect( changeset );

            var newCombatModel = GetUpdatedCombatModel( changeset );
            var events = _combatModelBuilder.GetCombatEvents( changeset.Effect, GetCommandersWithGolemInfo() );
            GenerateUpdatePackAndSetNewModel( newCombatModel, events );
        }

        //-------------------------------------------------------------------------------------[]
        private CombatModel GetUpdatedCombatModel( ClientChangesModel.Changeset changeset )
        {
            var newCombatModel = _combatModel.Clone();
            newCombatModel.ModelVersion = changeset.ModelVersion;
            _combatModelBuilder.UpdateModel( newCombatModel, changeset.Value );
            CheckRemainingTurnDuration( newCombatModel );
            return newCombatModel;
        }

        //-------------------------------------------------------------------------------------[]
        private void CheckRemainingTurnDuration( CombatModel newCombatModel )
        {
            if( newCombatModel.RemainingTurnDuration.HasValue )
                SetModelUpdateTime();
            else
                newCombatModel.RemainingTurnDuration = _combatModel.RemainingTurnDuration;
        }

        //-------------------------------------------------------------------------------------[]
        private void SetNewFullModel( CombatModel newCombatModel )
        {
            _combatUpdatePacks.Clear();
            GenerateUpdatePackAndSetNewModel( newCombatModel, GetNewCombatModelEvents() );
            if( !_isFirstFullCombatModelReceived )
                PlayerReady();
        }

        //-------------------------------------------------------------------------------------[]
        private static IEnumerable<ICombatEvent> GetNewCombatModelEvents()
        {
            return new ICombatEvent[] {new NewSituationEvent()};
        }

        //-------------------------------------------------------------------------------------[]
        private void GenerateUpdatePackAndSetNewModel(
            CombatModel newCombatModel,
            IEnumerable<ICombatEvent> events )
        {
            GenerateUpdatePack( newCombatModel, events );
            SetNewModel( newCombatModel );
        }

        //-------------------------------------------------------------------------------------[]
        private void GenerateUpdatePack(
            ICombatSituation newCombatModel,
            IEnumerable<ICombatEvent> events )
        {
            var pack = new CombatUpdatePack( _combatModel, newCombatModel, events, UpdatePackPostProcess );
            _combatUpdatePacks.Enqueue( pack );
            UpdatePackPreProcess( pack );
        }

        //-------------------------------------------------------------------------------------[]
        private void SetNewModel( CombatModel newCombatModel )
        {
            _combatModel = newCombatModel;
            if( _startCombatTime == DateTime.MinValue &&
                _combatModel.MatchTimeInSeconds.HasValue &&
                ( _combatModel.MatchTimeInSeconds.Value > 0 || ( IsCombatInProcess || _combatModel.StartingMatch ) ) )
                _startCombatTime = DateTime.Now.AddSeconds( -_combatModel.MatchTimeInSeconds.Value );
        }

        //-------------------------------------------------------------------------------------[]
        private void UpdatePackPostProcess( ICombatUpdatePack pack )
        {
            pack.Events.ForEach( EventPostProcess, pack );
            CombatSituation = pack.NewModel;
            GetHexHelper().UpdateMapItems( CombatSituation.HexMapItems );
        }

        //-------------------------------------------------------------------------------------[]
        private void UpdatePackPreProcess( ICombatUpdatePack pack )
        {
            pack.Events.ForEach( EventPreProcess, pack );
        }

        //-------------------------------------------------------------------------------------[]
        private void EventPostProcess(
            ICombatUpdatePack pack,
            ICombatEvent combatEvent )
        {
            Requirements.WriteToLogAfterProcess( combatEvent, pack.OldModel );
            switch( combatEvent.Type ) {
                case CombatEventType.GolemsAnimationEnded :
                    Requirements.SendGolemsAnimationEnded();
                    break;
                case CombatEventType.StartCombat :
                    _startCombatTime = DateTime.Now;
                    break;
                case CombatEventType.EndCombat :
                    _endCombatTime = DateTime.Now;
                    if( OnEndCombat != null )
                        OnEndCombat();
                    break;
            }
        }

        //-------------------------------------------------------------------------------------[]
        private void EventPreProcess(
            ICombatUpdatePack pack,
            ICombatEvent combatEvent )
        {
            switch( combatEvent.Type ) {
                case CombatEventType.EndCombat :
                    _matchId = combatEvent.Combat.Value;
                    _queueFighters = new QueueFighters[0];
                    break;

                case CombatEventType.StartRound : {
                    var newFighters = GetNewQueryFighters( pack,
                                                           combatEvent.Fighters );
                    _queueFighters =
                        _queueFighters.Union(
                                             newFighters.Where(
                                                               queueFighter => !_queueFighters.ExistsByProperty( f => f.FighterId,
                                                                                                                 queueFighter.
                                                                                                                     FighterId ) ) ).
                            ToArray();
                }
                    break;

                case CombatEventType.Death :
                    RemoveDeadGolemFromQueue( combatEvent.ActiveFighter );
                    break;
            }

            Requirements.WriteToLogBeforeProcess( combatEvent, pack.OldModel );
        }

        //-------------------------------------------------------------------------------------[]
        private IEnumerable<QueueFighters> GetNewQueryFighters(
            ICombatUpdatePack pack,
            IEnumerable<FighterId> fightersId )
        {
            var queryFightersList = new List<QueueFighters>();
            foreach( var fighterId in fightersId ) {
                var fighterInfo = GetFighter( fighterId );
                if( fighterInfo == null ||
                    !fighterInfo.FighterId.HasValue() )
                    fighterInfo = pack.NewModel.Fighters.FirstOrDefault( t => t.FighterId == fighterId );
                queryFightersList.Add( new QueueFighters {
                    Class = fighterInfo.Class,
                    FighterId = fighterId,
                    AuraColor = fighterInfo.AuraColor,
                    IsGolem = fighterInfo.IsGolem
                } );
            }
            return queryFightersList;
        }

        //-------------------------------------------------------------------------------------[]
        private void RemoveDeadGolemFromQueue( FighterId deadFighterId )
        {
            if( GetFighter( deadFighterId ) == null ||
                !GetFighter( deadFighterId ).IsGolem )
                return;
            var deadGolem = _queueFighters.FirstOrDefault( f => f.FighterId == deadFighterId );
            if( deadGolem == null )
                return;
            var tmpQueue = _queueFighters.ToList();
            tmpQueue.Remove( deadGolem );
            _queueFighters = tmpQueue.ToArray();
        }

        //===============================================================================================[]
        #endregion
    }
}