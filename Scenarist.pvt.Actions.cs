// Aleksey Pustohaylov
using System;
using System.Collections.Generic;
using Obs.Gunswords.dev.Common.ConstantValues;
using Obs.Gunswords.dev.Common.Types;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Combat.Pub.Types;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Imp.Stuff;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Pub.Out.Substances.Interfaces;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Pub.Out.Substances.Types;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Pub.Out.Types;
using Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Pub.Out.Workers;

namespace Obs.Gunswords.dev.Presentation.Essence.Simple.Theater.Imp.Workings
{
    public static class Roles
    {
        public const string Active = "Red";
        public const string Passive = "Blue";
    }

    internal partial class Scenarist : IScenarist
    {
        //-------------------------------------------------------------------------------------[]
        private readonly AuraColor _blueAuraColor = new AuraColor( "Blue" );
        private readonly AuraColor _redAuraColor = new AuraColor( "Red" );
        //-------------------------------------------------------------------------------------[]




        #region Routine
        //===============================================================================================[]
        private readonly Dictionary<StoryActionType, Action<IStoryAction, DraftScenario>> _writeActions =
            new Dictionary<StoryActionType, Action<IStoryAction, DraftScenario>>();

        //-------------------------------------------------------------------------------------[]
        private void InitializeWriteActions()
        {
            _writeActions.Add( StoryActionType.Casting, WriteAttackScenarioLine );
            _writeActions.Add( StoryActionType.PowerUpCasting, WritePowerUpCastingScenarioLine );
            _writeActions.Add( StoryActionType.Moving, WriteMovingScenarioLine );
            _writeActions.Add( StoryActionType.Running, WriteRunningScenarioLine );
            _writeActions.Add( StoryActionType.Shooting, WriteAttackScenarioLine );
            _writeActions.Add( StoryActionType.Striking, WriteAttackScenarioLine );
            _writeActions.Add( StoryActionType.StrikingCharge, WriteAttackChargeScenarioLine );
            _writeActions.Add( StoryActionType.Turning, WriteTurningScenarioLine );
            _writeActions.Add( StoryActionType.NewActor, WriteNewActorScenarioLine );
            _writeActions.Add( StoryActionType.ActiveFighter, WriteActiveFighterScenarioLine );
        }

        #region IScenarist
        //===============================================================================================[]
        public IDraftScenario WriteScenario(IEnumerable<IStoryAction> storyActions)
        {
            var draftScenario = new DraftScenario();
            storyActions.ForEach(WriteScenarioLine, draftScenario);
            return draftScenario;
        }
        //===============================================================================================[]
        #endregion

        //-------------------------------------------------------------------------------------[]        
        private void WriteScenarioLine(
            DraftScenario draftScenario,
            IStoryAction action )
        {
            if( !IsValidStoryAction( action ) )
                return;
            _writeActions[ action.Type ]( action, draftScenario );
        }

        //-------------------------------------------------------------------------------------[]
        private void SetActiveRoleAccord(
            DraftScenario draftScenario,
            ActorId actor )
        {
            _rolesAccord[ Roles.Active ] = draftScenario.GetActorRole( actor );
        }

        //-------------------------------------------------------------------------------------[]
        private void SetPassiveRoleAccord(
            DraftScenario draftScenario,
            ActorId actor )
        {
            _rolesAccord[ Roles.Passive ] = draftScenario.GetActorRole( actor );
        }

        //-------------------------------------------------------------------------------------[]
        private void SetBothRoleAccord(
            DraftScenario draftScenario,
            IStoryAction action )
        {
            SetActiveRoleAccord( draftScenario, action.ActiveActor );
            SetPassiveRoleAccord( draftScenario, action.PassiveActor );
        }

        //-------------------------------------------------------------------------------------[]
        private static AttackView GetAttackView( IStoryAction action )
        {
            if( action.ActiveActor ==
                action.PassiveActor )
                return AttackView.Self;
            if( action.IsFatality )
                return AttackView.Fatality;
            if( action.WithDeath )
                return AttackView.Death;
            if( action.WithoutDamage )
                return AttackView.Block;

            return AttackView.Live;
        }

        //===============================================================================================[]
        #endregion




        #region ScenarioLines
        //===============================================================================================[]
        private void WriteAttackScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            SetBothRoleAccord( draftScenario, action );
            SetLastCodeAsBase( draftScenario );
            var attackView = GetAttackView( action );
            switch( attackView ) {
                case AttackView.Self : {
                    AddAttackPiece( draftScenario, action, GetAttackSelfScenario( action ) );
                    break;
                }
                case AttackView.Fatality : {
                    AddAttackFatalityRedPiece( draftScenario, action );
                    AddAttackBluePiece( draftScenario, action, attackView );
                    break;
                }
                default : {
                    AddAttackRedPiece( draftScenario, action );
                    AddAttackBluePiece( draftScenario, action, attackView );
                    break;
                }
            }
        }

        //-------------------------------------------------------------------------------------[]
        private void WritePowerUpCastingScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            SetBothRoleAccord( draftScenario, action );
            SetLastCodeAsBase( draftScenario );
            var attackView = GetAttackView( action );

            if( action.EquipmentType ==
                Constants.Spells.Lightning ) {
                AddAttackPiece( draftScenario, action, GetAttackRedPowerUpPiece( action ) );
                AddAttackBluePiece( draftScenario, action, attackView );
            }
            else {
                switch( attackView ) {
                    case AttackView.Death :
                    case AttackView.Fatality :
                        AddAttackPiece( draftScenario, action, GetAttackBlueDeathScenario( action ) );
                        break;
                    default :
                        AddAttackPiece( draftScenario, action, GetAttackBlueLiveScenario( action ) );
                        break;
                }
            }
        }

        //-------------------------------------------------------------------------------------[]
        private void WriteAttackChargeScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            SetBothRoleAccord( draftScenario, action );
            SetLastCodeAsBase( draftScenario );
            var attackView = GetAttackView( action );

            AddAttackChargePieceForRed( draftScenario, action, GetAttackChargeRedScenario( action ) );
            AddAttackChargeBluePiece( draftScenario, action, attackView );
        }

        //-------------------------------------------------------------------------------------[]
        private void AddAttackChargeBluePiece(
            DraftScenario draftScenario,
            IStoryAction action,
            AttackView attackView )
        {
            switch( attackView ) {
                case AttackView.Live :
                    AddAttackChargePieceForBlue( draftScenario, action, GetAttackChargeBlueLiveScenario( action ) );
                    break;
                case AttackView.Death :
                    AddAttackChargePieceForBlue( draftScenario, action, GetAttackChargeBlueDeathScenario( action ) );
                    break;
                case AttackView.Fatality :
                    AddAttackChargePieceForBlue( draftScenario, action, GetAttackChargeBlueDeathScenario( action ) );
                    break;
                case AttackView.Block :
                    AddAttackChargePieceForBlue( draftScenario, action, GetAttackChargeBlueMissScenario( action ) );
                    break;
            }
        }

        //-------------------------------------------------------------------------------------[]
        private void AddAttackFatalityRedPiece(
            DraftScenario draftScenario,
            IStoryAction action )
        {
            var scenario = GetAttackFatalityRedScenario( action );
            var piece = ConvertHexesToFly( action, scenario );
            AddAttackPiece( draftScenario, action, piece );
        }

        //-------------------------------------------------------------------------------------[]
        private void AddAttackRedPiece(
            DraftScenario draftScenario,
            IStoryAction action )
        {
            var scenario = GetAttackRedScenario( action );
            var piece = ConvertHexesToFly( action, scenario );
            AddAttackPiece( draftScenario, action, piece );
        }

        //-------------------------------------------------------------------------------------[]
        private void AddAttackBluePiece(
            DraftScenario draftScenario,
            IStoryAction action,
            AttackView attackView )
        {
            switch( attackView ) {
                case AttackView.Live : {
                    var scenario = GetAttackBlueLiveScenario( action );
                    var piece = ConvertHexesToFly( action, scenario );
                    AddAttackPiece( draftScenario, action, piece );
                }
                    break;
                case AttackView.Death : {
                    var scenario = GetAttackBlueDeathScenario( action );
                    var piece = ConvertHexesToFly( action, scenario );
                    AddAttackPiece( draftScenario, action, piece );
                }
                    break;
                case AttackView.Block : {
                    var scenario = GetAttackBlueMissScenario( action );
                    var piece = ConvertHexesToFly( action, scenario );
                    AddAttackPiece( draftScenario, action, piece );
                }
                    break;
                case AttackView.Fatality : {
                    var scenario = GetAttackBlueFatalityScenario( action );
                    var piece = ConvertHexesToFly( action, scenario );
                    AddAttackPiece( draftScenario, action, piece );
                }
                    break;
            }
        }

        //-------------------------------------------------------------------------------------[]
        private void AddAttackPiece(
            DraftScenario draftScenario,
            IStoryAction action,
            string piece )
        {
            AddPiece( draftScenario, ConvertAttackScenario( action, piece ) );
        }

        //-------------------------------------------------------------------------------------[]
        private void AddAttackChargePieceForRed(
            DraftScenario draftScenario,
            IStoryAction action,
            string piece )
        {
            AddPiece( draftScenario, ConvertAttackChargeScenarioForRed( action, piece ) );
        }

        //-------------------------------------------------------------------------------------[]
        private void AddAttackChargePieceForBlue(
            DraftScenario draftScenario,
            IStoryAction action,
            string piece )
        {
            AddPiece( draftScenario, ConvertAttackChargeScenarioForBlue( action, piece ) );
        }

        //-------------------------------------------------------------------------------------[]
        private void WriteMovingScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            SetActiveRoleAccord( draftScenario, action.ActiveActor );
            SetLastCodeAsBase( draftScenario );
            var piece = ConvertFirstMovingScenario( action, GetMoveScenario() );
            AddPiece( draftScenario, piece );
        }

        //-------------------------------------------------------------------------------------[]
        private void WriteRunningScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            SetActiveRoleAccord( draftScenario, action.ActiveActor );
            SetLastCodeAsBase( draftScenario );
            var piece = ConvertFirstMovingScenario( action, GetRunScenario() );
            AddPiece( draftScenario, piece );
        }

        //-------------------------------------------------------------------------------------[]
        private void WriteTurningScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            SetActiveRoleAccord( draftScenario, action.ActiveActor );
            SetLastCodeAsBase( draftScenario );
            var piece = ConvertTurningScenario( action, GetTurnScenario() );
            AddPiece( draftScenario, piece );
        }

        //-------------------------------------------------------------------------------------[]
        private void WriteNewActorScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            SetActiveRoleAccord( draftScenario, action.ActiveActor );
            SetLastCodeAsBase( draftScenario );
            var piece = GetNewActorScenario();
            AddPiece( draftScenario, piece );
        }

        //-------------------------------------------------------------------------------------[]
        private void WriteActiveFighterScenarioLine(
            IStoryAction action,
            DraftScenario draftScenario )
        {
            if( !action.ActiveActor.HasValue() )
                return;
            SetActiveRoleAccord( draftScenario, action.ActiveActor );
            SetLastCodeAsBase( draftScenario );
            var piece = string.Empty;
            if( action.ActiveColor == _blueAuraColor )
                piece = GetChooseCharBlueScenario();
            if( action.ActiveColor == _redAuraColor )
                piece = GetChooseCharRedScenario();
            if( !string.IsNullOrEmpty( piece ) )
                AddPiece( draftScenario, piece );
        }

        //===============================================================================================[]
        #endregion
    }
}