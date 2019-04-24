using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using EntityStates.Headstompers;
using RoR2;
using RoR2.UI;
using UnityEngine;
using Frogtown;

namespace AdvancedTooltips
{
    [BepInDependency("com.frogtown.shared")]
    [BepInPlugin("com.orangutan.advancedtooltips", "AdvancedTooltips", "1.0.0")]
    public class AdvancedTooltips : BaseUnityPlugin
    {
        public ItemDef[] itemDefs;
        public ItemDef[] originalItemDefs;
        public bool LastEnabledState = false;

        public FrogtownModDetails modDetails;
        CharacterBody Body;
        Inventory Inventory;

        public void Awake()
        {
            modDetails = new FrogtownModDetails("com.orangutan.advancedtooltips")
            {
                description = "Gives advanced tooltips.",
                githubAuthor = "OrangutanGaming",
                githubRepo = "RoR2-AdvancedTooltips",
                thunderstoreFullName = "OrangutanGaming-AdvancedTooltips",
            };
            modDetails.OnlyContainsBugFixesOrUIChangesThatArentContriversial();
            FrogtownShared.RegisterMod(modDetails);

            itemDefs = typeof(ItemCatalog).GetStaticField<ItemDef[]>("itemDefs");
            originalItemDefs = itemDefs.ToArray();
        }

        public void Update()
        {
            if (LastEnabledState != modDetails.enabled)
            {
                LastEnabledState = modDetails.enabled;
                if (modDetails.enabled)
                {
                    // Just enabled
                    foreach (var itemDef in itemDefs)
                    {
                        itemDef.pickupToken = itemDef.descriptionToken;
                    }
                }
                else
                {
                    foreach (var itemDef in itemDefs)
                    {
                        foreach (var oItemDef in originalItemDefs)
                        {
                            if (oItemDef.descriptionToken == itemDef.descriptionToken)
                            {
                                itemDef.pickupToken = oItemDef.pickupToken;
                                break;
                            }
                        }
                    }
                }

                if (!modDetails.enabled)
                {
                    return;
                }

                var localUser = LocalUserManager.GetFirstLocalUser();
                if (localUser == null || localUser.cachedMasterController == null ||
                    localUser.cachedMasterController.master == null) return;
                var master = localUser.cachedMasterController.master;
                Body = master.GetBody();
                Inventory = master.inventory;
                var huds = typeof(HUD).GetStaticField<List<HUD>>("instancesList");
                foreach (var hud in huds)
                {
                    var items = hud.itemInventoryDisplay.GetField<List<ItemIcon>>("itemIcons");
                    foreach (var item in items)
                    {
                        var def = ItemCatalog.GetItemDef(item.GetField<ItemIndex>("itemIndex"));
                        var index = item.GetField<ItemIndex>("itemIndex");
                        var count = item.GetField<Int32>("itemCount");
                        item.tooltipProvider.overrideBodyText =
                            Language.GetString(def.descriptionToken) + GetExtraData(index, count);
                    }
                }
            }

            Double Clover(Double orig, Int32 cloverCount)
            {
                return (1f - Math.Pow((1f - orig / 100.0f), cloverCount + 1)) * 100.0f;
            }

            String GetExtraData(ItemIndex index, Int32 count)
            {
                var cloverCount = Inventory.GetItemCount(ItemIndex.Clover);
                switch (index)
                {
                    case ItemIndex.None:
                        break;
                    case ItemIndex.Syringe:
                        return "<br><br>From Syringes : " + Util.GenerateColoredString(count * 15 + "%", Color.green) +
                               "<br>Total Attack Speed Increase : " +
                               Util.GenerateColoredString(((Body.attackSpeed - 1) * 100) + "%", Color.green);
                    case ItemIndex.Bear:
                        var bearChance = (1f - 1f / (0.15f * count + 1f)) * 100f;
                        return "<br><br>Chance to Block : " +
                               Util.GenerateColoredString(bearChance.ToString("0.###") + "%", Color.green) +
                               "<br><br>Picking up another : " + Util.GenerateColoredString(
                                   ((1f - 1f / (0.15f * (count + 1f) + 1f)) * 100f).ToString("0.###") + "%",
                                   Color.green);
                    case ItemIndex.Behemoth:
                        return "<br><br>Actual Radius (tooltip is incorrect) : " +
                               Util.GenerateColoredString((1.5f + 2.5f * count) + "m", Color.green) +
                               "<br>Damage : " +
                               Util.GenerateColoredString((Body.damage * 0.6f).ToString("0.###"), Color.green);
                    case ItemIndex.Missile:
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(0.1f, cloverCount).ToString("0.###"), Color.green) +
                               "<br>Damage : " + Util.GenerateColoredString(
                                   (Body.damage * 3.0f * count).ToString("0.###"),
                                   Color.green);
                    case ItemIndex.ExplodeOnDeath:
                        return "<br><br>Radius : " +
                               Util.GenerateColoredString((12f + 2.4f * (count - 1)) + "m", Color.green) +
                               "<br>Damage : " + Util.GenerateColoredString(
                                   (Body.damage * 3.5f * (1f + (count - 1) * 0.8f)).ToString("0.###"), Color.green);
                    case ItemIndex.Dagger:
                        return "<br><br>3 Daggers<br>Damage (each), can crit : " +
                               Util.GenerateColoredString((Body.damage * 1.5f).ToString("0.###"), Color.green);
                    case ItemIndex.Tooth:
                        return "<br><br>Healing : " +
                               Util.GenerateColoredString((4 * count).ToString("0.###"), Color.green) +
                               "<br>Size? : " +
                               Util.GenerateColoredString(Math.Pow(count, 0.25f).ToString("0.###"), Color.green);
                    case ItemIndex.CritGlasses:
                        return "<br><br>From Glasses : " +
                               Util.GenerateColoredString((0.1f * count).ToString("0.###"), Color.green) +
                               "<br>Total Crit : " +
                               Util.GenerateColoredString((Body.crit).ToString("0.###"), Color.green);
                    case ItemIndex.Hoof:
                        return "<br><br>From Hoofs : " +
                               Util.GenerateColoredString((0.14f * count).ToString("0.###"), Color.green) +
                               "<br>Total Move Speed : " +
                               Util.GenerateColoredString((Body.moveSpeed).ToString("0.###"), Color.green) +
                               "<br><br>Quiet foot steps...";
                    case ItemIndex.Feather:
                        return "<br><br>Total Extra Jumps : " +
                               Util.GenerateColoredString((Body.maxJumpCount - 1).ToString(), Color.green);
                    case ItemIndex.AACannon:
                        break;
                    case ItemIndex.ChainLightning:
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(0.25f, cloverCount).ToString("0.###"), Color.green) +
                               "<br>Damage : " +
                               Util.GenerateColoredString((Body.damage * 0.8f).ToString("0.###"), Color.green) +
                               "<br>Bounces : " + Util.GenerateColoredString((2 * count).ToString(), Color.green) +
                               "<br>Range : " + Util.GenerateColoredString((2 * count) + "m", Color.green);
                    case ItemIndex.PlasmaCore:
                        break;
                    case ItemIndex.Seed:
                        return "<br><br>Heal on Crit : " +
                               Util.GenerateColoredString(count.ToString("0.###"), Color.green) +
                               "<br>Crit Chance : " +
                               Util.GenerateColoredString(Body.crit.ToString("0.###"), Color.green);
                    case ItemIndex.Icicle:
                        return "<br><br>Damage : " +
                               Util.GenerateColoredString(
                                   (Body.damage * (0.5f + 0.5f * count) * 0.25f).ToString("0.###"),
                                   Color.green) +
                               "<br>Time : 5s per kill, stacks";
                    case ItemIndex.GhostOnKill:
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(0.1f, cloverCount).ToString("0.###"), Color.green);
                    case ItemIndex.Mushroom:
                        return "<br><br>Radius : " +
                               Util.GenerateColoredString((1.5f + 1.5f * count) + "m", Color.green) +
                               "<br>Healing : " +
                               Util.GenerateColoredString((0.0225f + 0.0225f * count).ToString("0.###") + "%/s",
                                   Color.green) +
                               "<br>Of target, not owner";
                    case ItemIndex.Crowbar:
                        return "<br><br>Multiplier : " +
                               Util.GenerateColoredString((1.5f + 0.3f * (count - 1)).ToString("0.###"), Color.green);
                    case ItemIndex.LevelBonus:
                        break;
                    case ItemIndex.AttackSpeedOnCrit:
                        return "<br><br>Crit Chance : " +
                               Util.GenerateColoredString(Body.crit.ToString("0.###"), Color.green) +
                               "<br>Total Attack Speed : " +
                               Util.GenerateColoredString(((Body.attackSpeed - 1) * 100) + "%", Color.green) +
                               "<br>Duration : 2s per crit";
                    case ItemIndex.BleedOnHit:
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(
                                   (Clover(0.15f * count, cloverCount) * 100.0f).ToString("0.###") + "%", Color.green) +
                               "<br>Damage : tbd, weird math" +
                               "<br>Duration : 3s";
                    case ItemIndex.SprintOutOfCombat:
                        return "<br><br>Multipler : " +
                               Util.GenerateColoredString((0.3f * count).ToString("0.###"), Color.green) +
                               "<br>Total Move Speed : " +
                               Util.GenerateColoredString((Body.moveSpeed).ToString("0.###"), Color.green);
                    case ItemIndex.FallBoots:
                        return "<br><br>Cooldown : " +
                               Util.GenerateColoredString((HeadstompersCooldown.baseDuration / count).ToString("0.###"),
                                   Color.green);
                    case ItemIndex.CooldownOnCrit:
                        break;
                    case ItemIndex.WardOnLevel:
                        return "<br><br>Radius : " + Util.GenerateColoredString((8f + 8f * (count)) + "m", Color.green);
                    case ItemIndex.Phasing:
                        return "<br><br>Based on enemy damage, affected by clover" +
                               "<br> Duration, Speed : " +
                               Util.GenerateColoredString((1.5f + 1.5f * (count)).ToString(), Color.green);
                    case ItemIndex.HealOnCrit:
                        return "Healing : " + Util.GenerateColoredString((4 * 4 * count).ToString(), Color.green) +
                               "<br>Crit : " +
                               Util.GenerateColoredString((5 * count).ToString("0.###") + "%", Color.green) +
                               "<br>Total Crit : " +
                               Util.GenerateColoredString(Body.crit.ToString("0.###") + "%", Color.green);
                    case ItemIndex.HealWhileSafe:
                        return "<br><br>Regen : " +
                               Util.GenerateColoredString((2.5f + (count - 1) * 1.5f).ToString("0.###"), Color.green) +
                               "<br>Total Regen : " +
                               Util.GenerateColoredString(Body.regen.ToString("0.###") + "%", Color.green);
                    case ItemIndex.TempestOnKill:
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(0.25f, cloverCount).ToString("0.###"), Color.green) +
                               "<br>Duration : " +
                               Util.GenerateColoredString((2f + 6f * count).ToString("0.###") + "s", Color.green);
                    case ItemIndex.PersonalShield:
                        return "<br><br>Shield : " +
                               Util.GenerateColoredString((25 * count).ToString("0.###"), Color.green) +
                               "<br>Total Shield : " +
                               Util.GenerateColoredString(Body.maxShield.ToString("0.###") + "s", Color.green);
                    case ItemIndex.EquipmentMagazine:
                        var cd = Math.Pow(0.85f, count);
                        cd *= Math.Pow(0.5f, Inventory.GetItemCount(ItemIndex.AutoCastEquipment));
                        return "<br><br>Cooldown : " +
                               Util.GenerateColoredString((1 - cd).ToString("0.###"), Color.green);
                    case ItemIndex.NovaOnHeal:
                        return "";
                    case ItemIndex.ShockNearby:
                        return "<br><br>Clover affects crit<br>Bounces : " +
                               Util.GenerateColoredString((2 * count).ToString(), Color.green);
                    case ItemIndex.Infusion:
                        return "<br><br>Bonus : " +
                               Util.GenerateColoredString(Inventory.infusionBonus.ToString(), Color.green) + " / " +
                               Util.GenerateColoredString((100 * count).ToString(), Color.green);
                    case ItemIndex.WarCryOnCombat:
                        break;
                    case ItemIndex.Clover:
                        break;
                    case ItemIndex.Medkit:
                        return "Healing : " + Util.GenerateColoredString((10 * count).ToString(), Color.green);
                    case ItemIndex.Bandolier:
                        var bando = (1f - 1f / Mathf.Pow(count + 1, 0.33f));
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(bando, cloverCount).ToString("0.###"), Color.green);
                    case ItemIndex.BounceNearby:
                        var meathook = 1f - 100f / (100f + 20f * count);
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(meathook, cloverCount).ToString("0.###"), Color.green);
                    case ItemIndex.IgniteOnKill:
                        return "";
                    case ItemIndex.PlantOnHit:
                        break;
                    case ItemIndex.StunChanceOnHit:
                        var stun = 1f - 1f / (0.05f * count + 1f);
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(stun, cloverCount).ToString("0.###"), Color.green);
                    case ItemIndex.Firework:
                        return "";
                    case ItemIndex.LunarDagger:
                        return "";
                    case ItemIndex.GoldOnHit:
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(.30f, cloverCount).ToString("0.###"), Color.green) +
                               "<br>Gold : " + Util.GenerateColoredString(
                                   (2f * count * Run.instance.difficultyCoefficient).ToString("0.###"), Color.yellow);
                    case ItemIndex.MageAttunement:
                        break;
                    case ItemIndex.WarCryOnMultiKill:
                        return "";
                    case ItemIndex.BoostHp:
                        break;
                    case ItemIndex.BoostDamage:
                        break;
                    case ItemIndex.ShieldOnly:
                        return "";
                    case ItemIndex.AlienHead:
                        cd = Math.Pow(0.75f, count);
                        return "<br><br>Cooldown : " +
                               Util.GenerateColoredString((1 - cd).ToString("0.###"), Color.green);
                    case ItemIndex.Talisman:
                        return "";
                    case ItemIndex.Knurl:
                        break;
                    case ItemIndex.BeetleGland:
                        break;
                    case ItemIndex.BurnNearby:
                        break;
                    case ItemIndex.CritHeal:
                        break;
                    case ItemIndex.CrippleWardOnLevel:
                        break;
                    case ItemIndex.SprintBonus:
                        return "<br><br>Total Move Speed : " +
                               Util.GenerateColoredString((Body.moveSpeed).ToString("0.###"), Color.green);
                    case ItemIndex.SecondarySkillMagazine:
                        break;
                    case ItemIndex.StickyBomb:
                        return "<br><br>Chance : " +
                               Util.GenerateColoredString(Clover(2.5f + 2.5f * count, cloverCount).ToString("0.###"),
                                   Color.green);
                    case ItemIndex.TreasureCache:
                        var totalWeight = (80f + 20f * count + Math.Pow(count, 2f)) / 100.0f;
                        var totalWeight2 = (80f + 20f * (count + 1) + Math.Pow(count + 1, 2f)) / 100.0f;
                        return "<br><br>Every stage will have a rusted lockbox." +
                               "<br>Chance for White : " +
                               Util.GenerateColoredString((80f / totalWeight).ToString("0.###") + "%", Color.green) +
                               "<br>Chance for Green : " +
                               Util.GenerateColoredString((20f * count / totalWeight).ToString("0.###") + "%",
                                   Color.green) +
                               "<br>Chance for Red : " +
                               Util.GenerateColoredString((Math.Pow(count, 2) / totalWeight).ToString("0.###") + "%",
                                   Color.green) +
                               "<br><br>If you get one more key..." +
                               "<br>Chance for White : " +
                               Util.GenerateColoredString((80f / totalWeight2).ToString("0.###") + "%", Color.green) +
                               "<br>Chance for Green : " +
                               Util.GenerateColoredString((20f * (count + 1) / totalWeight2).ToString("0.###") + "%",
                                   Color.green) +
                               "<br>Chance for Red : " +
                               Util.GenerateColoredString(
                                   (Math.Pow(count + 1, 2) / totalWeight2).ToString("0.###") + "%",
                                   Color.green);
                    case ItemIndex.BossDamageBonus:
                        break;
                    case ItemIndex.SprintArmor:
                        break;
                    case ItemIndex.IceRing:
                        return "<br><br>Chance (shares proc with Kjaro's Band) : " +
                               Util.GenerateColoredString(Clover(8f, cloverCount).ToString("0.###"), Color.green);
                    case ItemIndex.FireRing:
                        return "<br><br>Chance (shares proc with Runald's Band) : " +
                               Util.GenerateColoredString(Clover(8f, cloverCount).ToString("0.###"), Color.green);
                    case ItemIndex.SlowOnHit:
                        break;
                    case ItemIndex.ExtraLife:
                        break;
                    case ItemIndex.ExtraLifeConsumed:
                        break;
                    case ItemIndex.UtilitySkillMagazine:
                        break;
                    case ItemIndex.HeadHunter:
                        break;
                    case ItemIndex.KillEliteFrenzy:
                        break;
                    case ItemIndex.RepeatHeal:
                        break;
                    case ItemIndex.Ghost:

                        //"<br><br>Spooky sound on kill, spooky ghost...";
                        break;
                    case ItemIndex.HealthDecay:
                        break;
                    case ItemIndex.AutoCastEquipment:
                        cd = Math.Pow(0.85f, count);
                        cd *= Math.Pow(0.5f, Inventory.GetItemCount(ItemIndex.AutoCastEquipment));
                        return "<br><br>Cooldown : " +
                               Util.GenerateColoredString((1 - cd).ToString("0.###"), Color.green);
                    case ItemIndex.IncreaseHealing:
                        break;
                    case ItemIndex.JumpBoost:
                        break;
                    case ItemIndex.DrizzlePlayerHelper:
                        break;
                    case ItemIndex.Count:
                        break;
                }

                return "";
            }
        }
    }
}