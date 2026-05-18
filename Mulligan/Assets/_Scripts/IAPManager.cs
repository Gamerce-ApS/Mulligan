using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System.Collections.Generic;
using Singular;
public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance { get; private set; }

#if UNITY_ANDROID
    public const string FullGameProductId = "full_game_unlock";

#else
    public const string FullGameProductId = "full-game-unlock";
#endif

    public const string FullGameUnlockedKey = "full_game_unlocked";

    private static IStoreController storeController;
    private static IExtensionProvider extensionProvider;

    public bool IsInitialized => storeController != null && extensionProvider != null;
    public bool IsFullGameUnlocked => PlayerPrefs.GetInt(FullGameUnlockedKey, 0) == 1;

    public event Action OnIAPInitialized;
    public event Action OnFullGameUnlockedEvent;
    public event Action<string> OnPurchaseFailedEvent;

    

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services init failed: " + e.Message);
            return;
        }

        if (IsInitialized)
            return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(FullGameProductId, ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyFullGame(System.Action onComplete)
    {
        OnFullGameUnlockedEvent = onComplete;
        if (IsFullGameUnlocked)
        {
            Debug.Log("Full game already unlocked.");
            return;
        }

        if (!IsInitialized)
        {
            Debug.LogWarning("IAP is not initialized yet.");
            OnPurchaseFailedEvent?.Invoke("IAP not initialized");
            return;
        }

        Product product = storeController.products.WithID(FullGameProductId);

        if (product == null)
        {
            Debug.LogWarning("Product not found: " + FullGameProductId);
            OnPurchaseFailedEvent?.Invoke("Product not found");
            return;
        }

        if (!product.availableToPurchase)
        {
            Debug.LogWarning("Product not available to purchase: " + FullGameProductId);
            OnPurchaseFailedEvent?.Invoke("Product not available");
            return;
        }

        storeController.InitiatePurchase(product);
    }

    public string GetLocalizedPrice()
    {
        if (!IsInitialized)
            return "...";

        Product product = storeController.products.WithID(FullGameProductId);

        if (product == null || product.metadata == null)
            return "...";

        return product.metadata.localizedPriceString;
    }

    public void RestorePurchases()
    {
#if UNITY_IOS || UNITY_STANDALONE_OSX
        if (!IsInitialized)
        {
            Debug.LogWarning("IAP is not initialized yet.");
            return;
        }

        IAppleExtensions apple = extensionProvider.GetExtension<IAppleExtensions>();
        apple.RestoreTransactions((result, message) =>
        {
            Debug.Log("Restore result: " + result + " | " + message);
        });
#else
                Debug.Log("RestorePurchases is only needed on Apple platforms.");
#endif
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;

        Debug.Log("IAP initialized.");
        RefreshOwnershipFromStore();

        OnIAPInitialized?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("IAP init failed: " + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError("IAP init failed: " + error + " | " + message);
    }

  public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
{
    Product product = args.purchasedProduct;
    string productId = product.definition.id;

    Debug.Log("Purchase success: " + productId);

    TrackPurchaseWithSingular(product);

    if (productId == FullGameProductId)
    {
        UnlockFullGame();
    }
    else
    {
        Debug.LogWarning("Unknown product purchased: " + productId);
    }

    return PurchaseProcessingResult.Complete;
}
private void TrackPurchaseWithSingular(Product product)
{
    if (product == null)
    {
        Debug.LogWarning("Singular IAP tracking skipped: product is null.");
        return;
    }

    try
    {
        string currency = "USD";
        double amount = 0.0;

        if (product.metadata != null)
        {
            if (!string.IsNullOrEmpty(product.metadata.isoCurrencyCode))
                currency = product.metadata.isoCurrencyCode;

            amount = Convert.ToDouble(product.metadata.localizedPrice);
        }

        var attributes = new Dictionary<string, object>
        {
            { "productSKU", product.definition.id },
            { "productName", product.metadata != null ? product.metadata.localizedTitle : product.definition.id },
            { "productCategory", product.definition.type.ToString() },
            { "productQuantity", 1 },
            { "productPrice", amount }
        };

        if (!string.IsNullOrEmpty(product.transactionID))
            attributes["transaction_id"] = product.transactionID;

        SingularSDK.Revenue(currency, amount, attributes);

        Debug.Log($"Sent revenue to Singular: {product.definition.id} | {currency} {amount}");
    }
    catch (Exception e)
    {
        Debug.LogError("Failed to send revenue to Singular: " + e.Message);
    }
}
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        string msg = $"Purchase failed: {product.definition.id} | {failureReason}";
        Debug.LogWarning(msg);
        OnPurchaseFailedEvent?.Invoke(msg);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        string msg = $"Purchase failed: {product.definition.id} | {failureDescription.reason} | {failureDescription.message}";
        Debug.LogWarning(msg);
        OnPurchaseFailedEvent?.Invoke(msg);
    }

private void RefreshOwnershipFromStore()
{
    if (!IsInitialized)
        return;

    Product product = storeController.products.WithID(FullGameProductId);

    if (product != null && product.hasReceipt)
    {
        UnlockFullGame();
    }
}

    private void UnlockFullGame()
    {
        bool wasUnlocked = IsFullGameUnlocked;

        PlayerPrefs.SetInt(FullGameUnlockedKey, 1);
        PlayerPrefs.Save();

        if (!wasUnlocked)
        {
            Debug.Log("Full game unlocked.");
            OnFullGameUnlockedEvent?.Invoke();
        }
    }
}