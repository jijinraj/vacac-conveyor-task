using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScannerDisplay : MonoBehaviour
{
    // ======================================================
    // UI REFERENCES
    // ======================================================

    [Header("UI References")]
    public GameObject scannerPanel;
    public Transform xrayGrid;
    public Image xrayItemPrefab;


    // ======================================================
    // SAFE ITEM SPRITES
    // ======================================================

    [Header("Safe Item Sprites")]
    public Sprite bookSprite;
    public Sprite phoneSprite;
    public Sprite bottleSprite;
    public Sprite hoodieSprite;
    public Sprite cameraSprite;


    // ======================================================
    // DANGEROUS ITEM SPRITES
    // ======================================================

    [Header("Dangerous Item Sprites")]
    public Sprite knifeSprite;
    public Sprite gunSprite;
    public Sprite bombSprite;


    // ======================================================
    // FALLBACK
    // ======================================================

    [Header("Fallback")]

    [Tooltip(
        "Displayed if an item exists in the cargo but its X-ray sprite " +
        "has not been assigned."
    )]
    public Sprite missingItemSprite;


    // ======================================================
    // SCAN SETTINGS
    // ======================================================

    [Header("Scan Settings")]
    public float displayDuration = 1f;


    // ======================================================
    // DEVELOPMENT TEST
    // ======================================================

    [Header("Development Test")]
    public bool showTestCargoOnStart = false;


    // ======================================================
    // RUNTIME
    // ======================================================

    private Coroutine activeScan;


    // ======================================================
    // UNITY
    // ======================================================

    void Start()
    {
        ValidateSpriteAssignments();


        if (scannerPanel != null)
        {
            scannerPanel.SetActive(false);
        }


        if (showTestCargoOnStart)
        {
            List<CargoItemType> testItems =
                new List<CargoItemType>
                {
                    CargoItemType.Book,
                    CargoItemType.Phone,
                    CargoItemType.Camera,
                    CargoItemType.Knife
                };


            ShowItems(testItems);
        }
    }


    // ======================================================
    // SHOW CARGO
    // ======================================================

    public void ShowCargo(
        CargoContents cargo
    )
    {
        if (cargo == null)
            return;


        ShowItems(
            cargo.items
        );
    }


    // ======================================================
    // SHOW ITEMS
    // ======================================================

    public void ShowItems(
        List<CargoItemType> items
    )
    {
        if (
            items == null ||
            scannerPanel == null ||
            xrayGrid == null ||
            xrayItemPrefab == null
        )
        {
            Debug.LogError(
                "ScannerDisplay cannot show cargo because one or more " +
                "required UI references are missing."
            );

            return;
        }


        if (activeScan != null)
        {
            StopCoroutine(
                activeScan
            );
        }


        // Copy the list so the scanner display is not affected
        // if the original cargo data changes or is destroyed.

        List<CargoItemType> scanItems =
            new List<CargoItemType>(
                items
            );


        activeScan =
            StartCoroutine(
                ShowItemsRoutine(
                    scanItems
                )
            );
    }


    // ======================================================
    // DISPLAY ROUTINE
    // ======================================================

    IEnumerator ShowItemsRoutine(
        List<CargoItemType> items
    )
    {
        ClearGrid();


        scannerPanel.SetActive(
            true
        );


        foreach (
            CargoItemType item
            in items
        )
        {
            Sprite sprite =
                GetSprite(
                    item
                );


            // --------------------------------------------------
            // NEVER SILENTLY HIDE AN ITEM
            // --------------------------------------------------

            if (sprite == null)
            {
                Debug.LogError(
                    $"SCANNER ERROR: No X-ray sprite assigned for {item}."
                );


                sprite =
                    missingItemSprite;
            }


            // If even the fallback is missing, log a very obvious
            // configuration error.

            if (sprite == null)
            {
                Debug.LogError(
                    $"SCANNER CRITICAL ERROR: {item} cannot be displayed " +
                    "because both its sprite and Missing Item Sprite are unassigned."
                );

                continue;
            }


            // --------------------------------------------------
            // CREATE X-RAY IMAGE
            // --------------------------------------------------

            Image image =
                Instantiate(
                    xrayItemPrefab,
                    xrayGrid
                );


            image.sprite =
                sprite;

            image.preserveAspect =
                true;

            image.color =
                Color.white;
        }


        yield return new WaitForSeconds(
            displayDuration
        );


        scannerPanel.SetActive(
            false
        );


        ClearGrid();


        activeScan =
            null;
    }


    // ======================================================
    // SPRITE LOOKUP
    // ======================================================

    Sprite GetSprite(
        CargoItemType item
    )
    {
        switch (item)
        {
            case CargoItemType.Book:
                return bookSprite;

            case CargoItemType.Phone:
                return phoneSprite;

            case CargoItemType.Bottle:
                return bottleSprite;

            case CargoItemType.Hoodie:
                return hoodieSprite;

            case CargoItemType.Camera:
                return cameraSprite;

            case CargoItemType.Knife:
                return knifeSprite;

            case CargoItemType.Gun:
                return gunSprite;

            case CargoItemType.Bomb:
                return bombSprite;

            default:
                return null;
        }
    }


    // ======================================================
    // VALIDATE SPRITES
    // ======================================================

    void ValidateSpriteAssignments()
    {
        ValidateSprite(
            bookSprite,
            CargoItemType.Book
        );


        ValidateSprite(
            phoneSprite,
            CargoItemType.Phone
        );


        ValidateSprite(
            bottleSprite,
            CargoItemType.Bottle
        );


        ValidateSprite(
            hoodieSprite,
            CargoItemType.Hoodie
        );


        ValidateSprite(
            cameraSprite,
            CargoItemType.Camera
        );


        ValidateSprite(
            knifeSprite,
            CargoItemType.Knife
        );


        ValidateSprite(
            gunSprite,
            CargoItemType.Gun
        );


        ValidateSprite(
            bombSprite,
            CargoItemType.Bomb
        );
    }


    void ValidateSprite(
        Sprite sprite,
        CargoItemType item
    )
    {
        if (sprite == null)
        {
            Debug.LogError(
                $"SCANNER CONFIGURATION ERROR: " +
                $"{item} does not have an assigned X-ray sprite."
            );
        }
    }


    // ======================================================
    // CLEAR GRID
    // ======================================================

    void ClearGrid()
    {
        if (xrayGrid == null)
            return;


        for (
            int i = xrayGrid.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                xrayGrid.GetChild(i);


            if (child != null)
            {
                Destroy(
                    child.gameObject
                );
            }
        }
    }
}