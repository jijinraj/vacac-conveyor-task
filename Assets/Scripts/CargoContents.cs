using System.Collections.Generic;
using UnityEngine;

public enum CargoItemType
{
    Book,
    Phone,
    Bottle,
    Hoodie,
    Camera,
    Knife,
    Gun,
    Bomb
}

public enum CargoRiskLevel
{
    Safe,
    Prohibited,
    Explosive
}

public class CargoContents : MonoBehaviour
{
    // ======================================================
    // GENERATED CONTENTS
    // ======================================================

    [Header("Hidden Cargo Contents")]

    public List<CargoItemType> items =
        new List<CargoItemType>();


    // ======================================================
    // GENERATION SETTINGS
    // ======================================================

    [Header("Random Generation")]

    public bool generateOnAwake = true;

    [Range(1, 5)]
    public int minimumItems = 1;

    [Range(1, 5)]
    public int maximumItems = 5;


    [Header("Threat Probabilities")]

    [Range(0f, 1f)]
    public float prohibitedChance = 0.20f;

    [Range(0f, 1f)]
    public float bombChance = 0.05f;


    // ======================================================
    // AVAILABLE SAFE ITEMS
    // ======================================================

    private readonly CargoItemType[] safeItems =
    {
        CargoItemType.Book,
        CargoItemType.Phone,
        CargoItemType.Bottle,
        CargoItemType.Hoodie,
        CargoItemType.Camera
    };


    // ======================================================
    // UNITY
    // ======================================================

    void Awake()
    {
        if (generateOnAwake)
        {
            GenerateRandomContents();
        }
    }


    // ======================================================
    // RANDOM GENERATION
    // ======================================================

    public void GenerateRandomContents()
    {
        items.Clear();


        int minItems =
            Mathf.Clamp(
                minimumItems,
                1,
                safeItems.Length
            );


        int maxItems =
            Mathf.Clamp(
                maximumItems,
                minItems,
                safeItems.Length
            );


        int totalItemCount =
            Random.Range(
                minItems,
                maxItems + 1
            );


        // --------------------------------------------------
        // DECIDE PACKAGE RISK
        // --------------------------------------------------

        CargoRiskLevel risk =
            ChooseRiskLevel();


        // --------------------------------------------------
        // ADD THREAT FIRST
        // --------------------------------------------------

        if (
            risk ==
            CargoRiskLevel.Explosive
        )
        {
            items.Add(
                CargoItemType.Bomb
            );
        }
        else if (
            risk ==
            CargoRiskLevel.Prohibited
        )
        {
            CargoItemType prohibitedItem =
                Random.value < 0.5f
                ? CargoItemType.Knife
                : CargoItemType.Gun;


            items.Add(
                prohibitedItem
            );
        }


        // --------------------------------------------------
        // FILL REMAINING SPACE WITH SAFE ITEMS
        // --------------------------------------------------

        List<CargoItemType> availableSafeItems =
            new List<CargoItemType>(
                safeItems
            );


        while (
            items.Count < totalItemCount &&
            availableSafeItems.Count > 0
        )
        {
            int index =
                Random.Range(
                    0,
                    availableSafeItems.Count
                );


            items.Add(
                availableSafeItems[index]
            );


            // Prevent duplicate safe items.
            availableSafeItems.RemoveAt(
                index
            );
        }


        // --------------------------------------------------
        // SHUFFLE
        // --------------------------------------------------
        //
        // This makes sure the dangerous item is not always
        // stored at index 0.

        ShuffleItems();


        // --------------------------------------------------
        // DEVELOPMENT LOG
        // --------------------------------------------------

        Debug.Log(
            $"Generated cargo [{gameObject.name}] " +
            $"Risk: {GetRiskLevel()} | " +
            $"Contents: {string.Join(", ", items)}"
        );
    }


    // ======================================================
    // RISK GENERATION
    // ======================================================

    CargoRiskLevel ChooseRiskLevel()
    {
        float roll =
            Random.value;


        if (
            roll <
            bombChance
        )
        {
            return CargoRiskLevel.Explosive;
        }


        if (
            roll <
            bombChance +
            prohibitedChance
        )
        {
            return CargoRiskLevel.Prohibited;
        }


        return CargoRiskLevel.Safe;
    }


    // ======================================================
    // SHUFFLE
    // ======================================================

    void ShuffleItems()
    {
        for (
            int i = items.Count - 1;
            i > 0;
            i--
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );


            CargoItemType temporary =
                items[i];


            items[i] =
                items[randomIndex];


            items[randomIndex] =
                temporary;
        }
    }


    // ======================================================
    // CARGO CLASSIFICATION
    // ======================================================

    public CargoRiskLevel GetRiskLevel()
    {
        if (ContainsBomb())
        {
            return CargoRiskLevel.Explosive;
        }


        if (ContainsProhibitedItem())
        {
            return CargoRiskLevel.Prohibited;
        }


        return CargoRiskLevel.Safe;
    }


    public bool IsDangerous()
    {
        return
            ContainsBomb() ||
            ContainsProhibitedItem();
    }


    public bool ContainsBomb()
    {
        return items.Contains(
            CargoItemType.Bomb
        );
    }


    public bool ContainsProhibitedItem()
    {
        return
            items.Contains(
                CargoItemType.Knife
            ) ||
            items.Contains(
                CargoItemType.Gun
            );
    }
}