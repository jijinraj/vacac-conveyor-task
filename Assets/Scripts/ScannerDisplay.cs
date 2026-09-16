using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScannerDisplay : MonoBehaviour
{
    [Header("UI References")]
    public GameObject scannerPanel;
    public Transform xrayGrid;
    public Image xrayItemPrefab;

    [Header("Safe Item Sprites")]
    public Sprite bookSprite;
    public Sprite phoneSprite;
    public Sprite bottleSprite;
    public Sprite hoodieSprite;
    public Sprite cameraSprite;

    [Header("Dangerous Item Sprites")]
    public Sprite knifeSprite;
    public Sprite gunSprite;
    public Sprite bombSprite;

    [Header("Scan Settings")]
    public float displayDuration = 2.5f;

    [Header("Development Test")]
    public bool showTestCargoOnStart = true;

    private Coroutine activeScan;

    void Start()
    {
        if (scannerPanel != null)
            scannerPanel.SetActive(false);

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

    public void ShowCargo(CargoContents cargo)
    {
        if (cargo == null)
            return;

        ShowItems(cargo.items);
    }

    public void ShowItems(List<CargoItemType> items)
    {
        if (
            items == null ||
            scannerPanel == null ||
            xrayGrid == null ||
            xrayItemPrefab == null
        )
        {
            return;
        }

        if (activeScan != null)
            StopCoroutine(activeScan);

        activeScan =
            StartCoroutine(
                ShowItemsRoutine(items)
            );
    }

    IEnumerator ShowItemsRoutine(
        List<CargoItemType> items
    )
    {
        ClearGrid();

        scannerPanel.SetActive(true);

        foreach (CargoItemType item in items)
        {
            Sprite sprite =
                GetSprite(item);

            if (sprite == null)
                continue;

            Image image =
                Instantiate(
                    xrayItemPrefab,
                    xrayGrid
                );

            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        yield return new WaitForSeconds(
            displayDuration
        );

        scannerPanel.SetActive(false);

        ClearGrid();

        activeScan = null;
    }

    Sprite GetSprite(CargoItemType item)
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
            Destroy(
                xrayGrid.GetChild(i).gameObject
            );
        }
    }
}