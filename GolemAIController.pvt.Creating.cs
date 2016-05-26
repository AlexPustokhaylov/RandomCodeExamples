// Aleksey Pustohaylov
using System;
using System.Collections.Generic;
using System.Linq;
using Obs.Gunswords.dev.Common.DataTransferProtocol;
using Obs.Gunswords.dev.Common.Hexes.Pub;
using Obs.Gunswords.dev.Common.Types;
using Obs.Gunswords.dev.Schema.Old.Data;

namespace Obs.Gunswords.dev.Schema.Old
{
    // ReSharper disable InconsistentNaming
    internal sealed partial class GolemAIController // ReSharper restore InconsistentNaming
    {
        #region Creating
        //===============================================================================================[]
        private static readonly Random Rand = new Random();
        //-------------------------------------------------------------------------------------[]
        private IFighter DoCreateGolem(
            IScene scene,
            string golemClass )
        {
            var golem = GenerateFighterGolem( scene, golemClass );
            RegisterOwnership( golem );
            scene.AddFighter( golem );
            return golem;
        }

        //-------------------------------------------------------------------------------------[]
        private IFighter GenerateFighterGolem(
            IScene scene,
            string golemClass )
        {
            var golem = new Fighter( GetGolemStartingInfo( scene, golemClass ), scene.GameMod ) {Owner = Fighter.GolemsOwner};
            golem.CurrentWeaponId = golem.Weapons.First().WeaponId;
            SetGolemPosition( scene, golem );
            return golem;
        }

        //-------------------------------------------------------------------------------------[]
        private IFighterStartingInfo GetGolemStartingInfo(
            IPlayground scene,
            string golemClass )
        {
            return new FighterStartingInfo( BrigadeId, GetGolemFighterData( scene, golemClass ) );
        }

        //-------------------------------------------------------------------------------------[]
        private BrigadeData.Fighter DoCreateGolemGunner( IScene scene )
        {
            var golem = GenerateGolem( scene, CombatModelConstants.FighterClasses.GolemGunner );
            return golem;
        }

        //-------------------------------------------------------------------------------------[]
        private BrigadeData.Fighter DoCreateGolemKnight( IScene scene )
        {
            var golem = GenerateGolem( scene, CombatModelConstants.FighterClasses.GolemKnight );
            return golem;
        }

        //-------------------------------------------------------------------------------------[]
        private BrigadeData.Fighter GenerateGolem(
            IPlayground scene,
            string golemClass )
        {
            return GetGolemFighterData( scene, golemClass );
        }

        //-------------------------------------------------------------------------------------[]
        private static void RegisterOwnership( IEstate golem )
        {
            OwnershipManager.Instance.RegisterOwnership( golem.Owner, golem );
        }

        //-------------------------------------------------------------------------------------[]
        private static void SetGolemPosition(
            IScene scene,
            IScenicPoint golem )
        {
            var pos = GetFreeHex( scene );
            golem.Row = pos.Row;
            golem.Col = pos.Column;
        }

        //-------------------------------------------------------------------------------------[]
        private BrigadeData.Fighter GetGolemFighterData(
            IPlayground scene,
            string golemClass )
        {
            var loadedGolem = ServerHelper.Instance.GetRandomFighter( golemClass, scene.GamePointsLimit );
            var golem = new BrigadeData.Fighter {
                Class = golemClass,
                Name = ( golemClass == CombatModelConstants.FighterClasses.GolemReaper
                             ? GenerateGolemReaperName()
                             : GenerateGolemName() ),
                FighterCost = loadedGolem.PriceGamePoints,
                SceneWeightGamePoints = loadedGolem.SceneWeightGamePoints,
                HealthPoints = loadedGolem.HealthPoints,
                Armor =
                    new EquippedFighterData.ArmorParametersData {
                        ArmorId = loadedGolem.Armor.ArmorId,
                        MagicDamageReductionInPercent = loadedGolem.Armor.MagicDamageReductionInPercent,
                        PhysicalDamageReductionInPercent = loadedGolem.Armor.PhysicalDamageReductionInPercent,
                    },
                WeaponOrPrimarySpell =
                    new EquippedFighterData.WeaponOrPrimarySpellParametersData {
                        WeaponId = loadedGolem.WeaponOrPrimarySpell.WeaponId,
                        MaxPower = loadedGolem.WeaponOrPrimarySpell.MaxPower,
                        MinPower = loadedGolem.WeaponOrPrimarySpell.MinPower,
                        Range = loadedGolem.WeaponOrPrimarySpell.Range,
                        Falloff = loadedGolem.WeaponOrPrimarySpell.Falloff,
                    },
                ActionPoints = loadedGolem.ActionPoints,
                FogOfWarVisionRadius = loadedGolem.VisionRadius,
                IsReaper = golemClass == CombatModelConstants.FighterClasses.GolemReaper
            };
            return golem;
        }

        //-------------------------------------------------------------------------------------[]
        private string GetGolemClass()
        {
            return _rand.Next( 2 ) == 0
                       ? CombatModelConstants.FighterClasses.GolemKnight
                       : CombatModelConstants.FighterClasses.GolemGunner;
        }

        //-------------------------------------------------------------------------------------[]
        private string GenerateGolemName()
        {
            return GolemName + _rand.Next( MinRand, MaxRand + 1 );
        }

        //-------------------------------------------------------------------------------------[]
        private string GenerateGolemReaperName()
        {
            return GolemReaperName + _rand.Next( MinRand, MaxRand + 1 );
        }

        //===============================================================================================[]
        #endregion




        #region Routines
        //===============================================================================================[]
        private static HexIndex GetFreeHex( IScene scene )
        {
            var availableHexes =
                scene.HexHelper.PassableHexes.Where(
                    hex =>
                    !scene.IsAnyPowerUpInHex( hex ) && !scene.PowerUpRespawnsHexIndexes.Any( r => r.Row == hex.HexIndex.Row ) &&
                    !scene.PowerUpRespawnsHexIndexes.Any( c => c.Column == hex.HexIndex.Column ) ).ToList();
            var availableHexesFarThen9HexesFromPrevPowerUpHexIndex = new List<IHex>();
            if( scene.PrevGolemHexIndex != null ) {
                availableHexesFarThen9HexesFromPrevPowerUpHexIndex.AddRange(
                    availableHexes.Where(
                        hex =>
                        hex.HexIndex.DistanceTo(
                            new HexIndex( scene.PrevGolemHexIndex.Value.Row, scene.PrevGolemHexIndex.Value.Column ) ) >= 10 ) );
            }
            if( availableHexesFarThen9HexesFromPrevPowerUpHexIndex.Count > 0 ) {
                availableHexes.Clear();
                availableHexes.AddRange( availableHexesFarThen9HexesFromPrevPowerUpHexIndex );
            }
            var hexesInFogOfWar = availableHexes.Where( t => !scene.CanPlayersSeeHex( t.HexIndex ) );
            return hexesInFogOfWar.Any()
                       ? hexesInFogOfWar.GetAny( Rand ).HexIndex
                       : availableHexes.GetAny( Rand ).HexIndex;
        }

        //===============================================================================================[]
        #endregion
    }
}