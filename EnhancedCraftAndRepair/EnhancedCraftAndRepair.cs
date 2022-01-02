using HarmonyLib;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 7 Days To Die game modification.
/// </summary>
public class EnhancedCraftAndRepair : IModApi
{
    /// <summary>
    /// Mod initialization.
    /// </summary>
    /// <param name="_modInstance"></param>
    public void InitMod(Mod _modInstance)
    {
        Debug.Log("Loading mod: " + GetType().ToString());
        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// The Harmony patch for the method <see cref="XUiC_CraftingQueue.AddItemToRepair"/>.
    /// </summary>
    [HarmonyPatch(typeof(XUiC_CraftingQueue))]
    [HarmonyPatch("AddItemToRepair")]
    public class XUiC_CraftingQueue_AddItemToRepair
    {
        /// <summary>
        /// The additional code to execute after the original method <see cref="XUiC_CraftingQueue.AddItemToRepair"/>.
        /// Items to repair are always placed at the top of the queue.
        /// </summary>
        /// <param name="__instance"></param>
        public static void Postfix(XUiC_CraftingQueue __instance, ref XUiController[] ___queueItems, ref bool __result)
        {
            //Debug.LogErrorFormat($"XUiC_CraftingQueue.AddItemToRepair : called!");
            if (__result)
            {
                if (___queueItems.Length > 1)
                {
                    __instance.HaltCrafting();
                    XUiC_RecipeStack repairingItem = null;

                    for (int i = 0; i < ___queueItems.Length - 1; i++)
                    {
                        if (___queueItems[i] is XUiC_RecipeStack recipeStack && recipeStack.HasRecipe())
                        {
                            if (repairingItem == null)
                            {
                                repairingItem = new XUiC_RecipeStack();
                                recipeStack.CopyTo(repairingItem);
                            }

                            if (repairingItem != null)
                            {
                                ((XUiC_RecipeStack)___queueItems[i + 1]).CopyTo(recipeStack);
                            }
                        }
                    }

                    if (repairingItem != null)
                    {
                        repairingItem.CopyTo((XUiC_RecipeStack)___queueItems[___queueItems.Length - 1]);
                    }

                    __instance.ResumeCrafting();
                }
            }
        }
    }

    /// <summary>
    /// The Harmony patch for the method <see cref="XUiC_RecipeStack.Init"/>.
    /// </summary>
    [HarmonyPatch(typeof(XUiC_RecipeStack))]
    [HarmonyPatch("Init")]
    public class XUiC_RecipeStack_Init
    {
        /// <summary>
        /// The additional code to execute after the original method <see cref="XUiC_RecipeStack.Init"/>.
        /// Moves right-clicked item to the top of the crafting queue.
        /// </summary>
        /// <param name="__instance"></param>
        public static void Postfix(XUiC_RecipeStack __instance, XUiController ___background)
        {
            //Debug.LogErrorFormat($"XUiC_RecipeStack.Init : called!");
            if (___background != null)
            {
                ___background.OnRightPress += new XUiEvent_OnPressEventHandler((controller, _) => {
                    if (__instance.GetRecipe() != null)
                    {
                        var craftingQueueFieldInfo = typeof(XUiC_CraftingQueue).GetField("queueItems", BindingFlags.NonPublic | BindingFlags.Instance);
                        var craftingQueue = (XUiController[])craftingQueueFieldInfo.GetValue(__instance.Owner);
                        XUiC_RecipeStack tempItem = null;

                        if (craftingQueue != null && craftingQueue.Length > 1)
                        {
                            __instance.Owner.HaltCrafting();

                            for (int i = 0; i < craftingQueue.Length - 1; i++)
                            {
                                if (craftingQueue[i] is XUiC_RecipeStack recipeStack && recipeStack.HasRecipe())
                                {
                                    if (__instance == recipeStack)
                                    {
                                        tempItem = new XUiC_RecipeStack();
                                        recipeStack.CopyTo(tempItem);
                                    }

                                    if (tempItem != null)
                                    {
                                        ((XUiC_RecipeStack)craftingQueue[i + 1]).CopyTo(recipeStack);
                                    }
                                }
                            }

                            if (tempItem != null)
                            {
                                tempItem.CopyTo((XUiC_RecipeStack)craftingQueue[craftingQueue.Length - 1]);
                            }

                            __instance.Owner.ResumeCrafting();
                        }
                    }
                });
            }
        }
    }

    /// <summary>
    /// The Harmony patch for the method <see cref="XUiC_Toolbelt.Update"/>.
    /// </summary>
    [HarmonyPatch(typeof(XUiC_Toolbelt))]
    [HarmonyPatch("Update")]
    public class XUiC_Toolbelt_Update
    {
        public static bool RepairingFlag = false;

        /// <summary>
        /// The additional code to execute after the original method <see cref="XUiC_CraftingQueue.AddItemToRepair"/>.
        /// Repairs current weapon/tool in hands by mouse wheel click.
        /// </summary>
        /// <param name="__instance"></param>
        public static void Postfix(XUiC_Toolbelt __instance, XUiController[] ___itemControllers, int ___currentHoldingIndex)
        {
            if (Input.GetMouseButton(2)) // Middle button pressed (wheel)
            {
                if (!RepairingFlag)
                {
                    RepairingFlag = true;

                    if (___currentHoldingIndex != __instance.xui.PlayerInventory.Toolbelt.DUMMY_SLOT_IDX)
                    {
                        var currentItem = (XUiC_ItemStack)___itemControllers[___currentHoldingIndex];
                        var itemValue = currentItem?.ItemStack?.itemValue;

                        if (itemValue != null && itemValue.MaxUseTimes > 0 && itemValue.UseTimes > 0f && itemValue.ItemClass.RepairTools != null && itemValue.ItemClass.RepairTools.Length > 0 && itemValue.ItemClass.RepairTools[0].Value.Length > 0)
                        {
                            var repairAction = new ItemActionEntryRepair(currentItem);
                            repairAction.OnActivated();
                        }
                    }
                }

            }
            else
            {
                RepairingFlag = false;
            }
        }
    }
}
