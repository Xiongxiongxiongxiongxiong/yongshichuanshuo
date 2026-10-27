using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Observe real gameplay action clocks after the exhaustive pose tests.</summary>
public static class ValidateBladeGameplay
{
    static PlayerInventory inventory;
    static PlayerController player;
    static TestWeaponSwap swap;
    static InventoryItemDefinition[] items;
    static readonly HashSet<string> observed = new HashSet<string>();
    static readonly List<string> results = new List<string>();
    static int itemIndex, action, startFrame;
    static double deadline;
    static bool starting, hadSave;
    static string saved;
    static Vector3 position;
    static float stamina;
    public static bool Running { get; private set; }
    public static string Result { get; private set; }

    public static string Begin()
    {
        if (Running || !Application.isPlaying) throw new InvalidOperationException("Enter Play Mode and run once.");
        inventory = UnityEngine.Object.FindObjectOfType<PlayerInventory>(); player = inventory.GetComponent<PlayerController>(); swap = player.GetComponent<TestWeaponSwap>();
        items = inventory.itemDefinitions.Where(i => i != null && i.IsWeapon).ToArray();
        if (items.Length != 20) throw new InvalidOperationException("Expected ten swords plus ten dao.");
        saved = PlayerPrefs.GetString(PlayerInventory.SaveKey, ""); hadSave = PlayerPrefs.HasKey(PlayerInventory.SaveKey);
        position = player.transform.position; stamina = player.GetComponent<PlayerStamina>().Current;
        itemIndex = action = 0; starting = true; results.Clear(); observed.Clear();
        deadline = EditorApplication.timeSinceStartup + .3; Running = true; Result = "Running";
        EditorApplication.update += Tick; return "Observing thrust, whirlwind, slide and normal attack for all twenty weapons.";
    }
    static void Tick()
    {
        if (!Running) return;
        try
        {
            if (!Application.isPlaying || player == null) throw new InvalidOperationException("Play Mode ended before completion.");
            double now = EditorApplication.timeSinceStartup;
            if (starting)
            {
                if (now < deadline) return;
                var item = items[itemIndex]; string message;
                if (inventory.EquippedWeaponId != item.id)
                {
                    int index = inventory.Backpack.stacks.FindIndex(s => s.itemId == item.id);
                    if (!inventory.Equip(index, out message)) throw new InvalidOperationException("Equip failed: " + item.id);
                }
                player.GetComponent<PlayerStamina>().ResetStamina(); player.GetComponent<PlayerSwordSkills>().ResetSkills();
                observed.Clear(); startFrame = Time.frameCount;
                bool accepted = true;
                if (action < 2) accepted = player.GetComponent<PlayerSwordSkills>().TryUseSkill(action == 1);
                else if (action == 2) accepted = player.GetComponent<PlayerSlide>().TryStartSlide();
                else { player.AttackFromHud(); accepted = player.isAttack; }
                if (!accepted) throw new InvalidOperationException("Action rejected: " + item.id + "/" + action);
                deadline = now + new[] { .5, .8, .7, 1.35 }[action]; starting = false; return;
            }
            bool active = action < 2 ? player.GetComponent<PlayerSwordSkills>().IsUsingSkill : action == 2 ? player.GetComponent<PlayerSlide>().IsSliding : player.isAttack;
            if (active && Time.frameCount > startFrame && swap.CurrentBinding != null)
            {
                if (swap.CurrentWeaponFrame == null || !AssetDatabase.GetAssetPath(swap.CurrentWeaponFrame.texture).Contains("/" + items[itemIndex].id.Substring(items[itemIndex].id.IndexOf('.') + 1) + "/"))
                    throw new InvalidOperationException("Wrong/missing weapon in gameplay: " + items[itemIndex].id);
                observed.Add(swap.CurrentBinding.source.name);
            }
            if (now < deadline) return;
            if (observed.Count < 2) throw new InvalidOperationException("Action did not animate multiple frames: " + items[itemIndex].id + "/" + action);
            results.Add(items[itemIndex].id + "," + new[] { "U thrust", "I whirlwind", "K slide", "J attack" }[action] + "," + observed.Count + " body/weapon poses");
            player.GetComponent<PlayerSwordSkills>().ResetSkills(); player.GetComponent<PlayerSlide>().EndSlide();
            player.transform.position = position; player.GetComponent<Rigidbody2D>().velocity = Vector2.zero; Physics2D.SyncTransforms();
            action++; if (action == 4) { action = 0; itemIndex++; }
            if (itemIndex == items.Length) { Finish(null); return; }
            starting = true; deadline = now + .25;
        }
        catch (Exception e) { Finish(e.Message); }
    }
    static void Finish(string error)
    {
        EditorApplication.update -= Tick; Running = false;
        Result = error == null ? "PASS: eighty real gameplay action sequences across twenty weapons." : "FAIL: " + error;
        results.Add(Result); Directory.CreateDirectory("Docs/AI/WeaponCollection"); File.WriteAllLines("Docs/AI/WeaponCollection/GameplayValidation.txt", results);
        if (hadSave) PlayerPrefs.SetString(PlayerInventory.SaveKey, saved); else PlayerPrefs.DeleteKey(PlayerInventory.SaveKey);
        PlayerPrefs.Save();
        if (inventory != null) inventory.Load();
        if (player != null)
        {
            player.GetComponent<PlayerSwordSkills>().ResetSkills(); player.GetComponent<PlayerSlide>().EndSlide(); player.transform.position = position;
            var s = player.GetComponent<PlayerStamina>(); s.ResetStamina(); s.TrySpend(s.maxStamina - stamina);
        }
        Debug.Log(Result);
    }
}
