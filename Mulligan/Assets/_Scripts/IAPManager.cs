using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System.Collections.Generic;
using Singular;
using GameAnalyticsSDK;
public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance { get; private set; }

#if UNITY_ANDROID
    public const string FullGameProductId = "full_game_unlock";

#else
    public const string FullGameProductId = "full-game-unlock";
#endif

    public const string FullGameUnlockedKey = "full_game_unlocked";
    private const string HeroProductPrefix = "hero_";
    private const string HeroProductSuffix = "_unlock";
    private const string HeroUnlockedPrefix = "hero_";
    private const string HeroUnlockedSuffix = "_unlocked";
    private const int FirstPaidHeroIndex = 1;
    private const int LastPaidHeroIndex = 3;

    private static IStoreController storeController;
    private static IExtensionProvider extensionProvider;

    public bool IsInitialized => storeController != null && extensionProvider != null;
    public bool IsFullGameUnlocked => PlayerPrefs.GetInt(FullGameUnlockedKey, 0) == 1;

    public event Action OnIAPInitialized;
    public event Action OnFullGameUnlockedEvent;
    private event Action OnHeroUnlockedEvent;
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
        TrackDesign("iap:init:start");
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services init failed: " + e.Message);
            TrackDesign("iap:init:fail");
            TrackDesign("iap:init:fail:unity_services");
            return;
        }

        if (IsInitialized)
            return;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(FullGameProductId, ProductType.NonConsumable);
        for (int i = FirstPaidHeroIndex; i <= LastPaidHeroIndex; i++)
            builder.AddProduct(GetHeroProductId(i), ProductType.NonConsumable);

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

    public bool IsHeroUnlocked(int heroIndex)
    {
        if (heroIndex <= 0)
            return true;

        if (IsFullGameUnlocked)
            return true;

        return PlayerPrefs.GetInt(GetHeroUnlockedKey(heroIndex), 0) == 1;
    }

    public bool HasBoughtAnyIAP()
    {
        if (IsFullGameUnlocked)
            return true;

        for (int i = FirstPaidHeroIndex; i <= LastPaidHeroIndex; i++)
        {
            if (PlayerPrefs.GetInt(GetHeroUnlockedKey(i), 0) == 1)
                return true;
        }

        return false;
    }

    public void BuyHero(int heroIndex, System.Action onComplete)
    {
        OnHeroUnlockedEvent = onComplete;
        if (IsHeroUnlocked(heroIndex))
        {
            Debug.Log("Hero already unlocked: " + heroIndex);
            return;
        }

        if (!IsInitialized)
        {
            Debug.LogWarning("IAP is not initialized yet.");
            OnPurchaseFailedEvent?.Invoke("IAP not initialized");
            return;
        }

        string productId = GetHeroProductId(heroIndex);
        Product product = storeController.products.WithID(productId);

        if (product == null)
        {
            Debug.LogWarning("Product not found: " + productId);
            OnPurchaseFailedEvent?.Invoke("Product not found");
            return;
        }

        if (!product.availableToPurchase)
        {
            Debug.LogWarning("Product not available to purchase: " + productId);
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

    public string GetLocalizedHeroPrice(int heroIndex)
    {
        if (!IsInitialized)
            return "...";

        Product product = storeController.products.WithID(GetHeroProductId(heroIndex));

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
        TrackDesign("iap:init:success");
        RefreshOwnershipFromStore();

        OnIAPInitialized?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("IAP init failed: " + error);
        TrackDesign("iap:init:fail");
        TrackInitializeFailedReason(error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError("IAP init failed: " + error + " | " + message);
        TrackDesign("iap:init:fail");
        TrackInitializeFailedReason(error);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Product product = args.purchasedProduct;
        string productId = product.definition.id;
        int heroIndex = GetHeroIndexFromProductId(productId);

        Debug.Log("Purchase success: " + productId);

        TrackPurchaseWithSingular(product);

        if (productId == FullGameProductId)
        {
            TrackPurchaseWithGameAnalytics(product);
            UnlockFullGame();
        }
        else if (heroIndex >= 0)
        {
            TrackPurchaseWithGameAnalytics(product);
            UnlockHero(heroIndex);
        }
        else
        {
            Debug.LogWarning("Unknown product purchased: " + productId);
        }

        return PurchaseProcessingResult.Complete;
    }

    private void TrackPurchaseWithGameAnalytics(Product product)
    {
        if (product == null)
        {
            Debug.LogWarning("GameAnalytics IAP tracking skipped: product is null.");
            return;
        }

        if (GameAnalytics.Initialized == false)
            GameAnalytics.Initialize();

        GameAnalytics.NewDesignEvent("purchase:success");
        GameAnalytics.NewDesignEvent("purchase:success:" + product.definition.id);

        string currency = "USD";
        int amount = 0;

        if (product.metadata != null)
        {
            if (!string.IsNullOrEmpty(product.metadata.isoCurrencyCode))
                currency = product.metadata.isoCurrencyCode;

            amount = Mathf.RoundToInt((float)product.metadata.localizedPrice * 100f);
        }

        if (amount <= 0)
        {
            Debug.LogWarning("GameAnalytics business event skipped: invalid product price.");
            return;
        }

        GameAnalytics.NewBusinessEvent(currency, amount, "iap", product.definition.id, "shop");
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
        string productId = product != null ? product.definition.id : "unknown";
        string msg = $"Purchase failed: {productId} | {failureReason}";
        Debug.LogWarning(msg);
        TrackDesign("purchase:fail");
        TrackDesign("purchase:fail:" + productId);
        OnPurchaseFailedEvent?.Invoke(msg);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        string productId = product != null ? product.definition.id : "unknown";
        string msg = $"Purchase failed: {productId} | {failureDescription.reason} | {failureDescription.message}";
        Debug.LogWarning(msg);
        TrackDesign("purchase:fail");
        TrackDesign("purchase:fail:" + productId);
        OnPurchaseFailedEvent?.Invoke(msg);
    }

    private void RefreshOwnershipFromStore()
    {
        if (!IsInitialized)
            return;

        RefreshProductOwnership(FullGameProductId);

        for (int i = FirstPaidHeroIndex; i <= LastPaidHeroIndex; i++)
            RefreshProductOwnership(GetHeroProductId(i));
    }

    private void RefreshProductOwnership(string productId)
    {
        Product product = storeController.products.WithID(productId);
        if (product == null)
        {
            TrackDesign("iap:product:missing");
            TrackDesign("iap:product:missing:" + productId);
            return;
        }

        if (product.availableToPurchase)
        {
            TrackDesign("iap:product:available");
            TrackDesign("iap:product:available:" + productId);
        }
        else
        {
            TrackDesign("iap:product:missing");
            TrackDesign("iap:product:missing:" + productId);
        }

        if (product.hasReceipt)
        {
            if (productId == FullGameProductId)
                UnlockFullGame();
            else
                UnlockHero(GetHeroIndexFromProductId(productId));
        }
    }

    private void TrackInitializeFailedReason(InitializationFailureReason error)
    {
        if (error == InitializationFailureReason.PurchasingUnavailable)
            TrackDesign("iap:init:fail:purchasing_unavailable");
        else if (error == InitializationFailureReason.NoProductsAvailable)
            TrackDesign("iap:init:fail:no_products_available");
        else if (error == InitializationFailureReason.AppNotKnown)
            TrackDesign("iap:init:fail:app_not_known");
        else
            TrackDesign("iap:init:fail:unknown");
    }

    private void TrackDesign(string eventName)
    {
        if (GameAnalytics.Initialized == false)
            GameAnalytics.Initialize();

        GameAnalytics.NewDesignEvent(eventName);
    }

    private string GetHeroProductId(int heroIndex)
    {
        return HeroProductPrefix + heroIndex + HeroProductSuffix;
    }

    private string GetHeroUnlockedKey(int heroIndex)
    {
        return HeroUnlockedPrefix + heroIndex + HeroUnlockedSuffix;
    }

    private int GetHeroIndexFromProductId(string productId)
    {
        for (int i = FirstPaidHeroIndex; i <= LastPaidHeroIndex; i++)
        {
            if (productId == GetHeroProductId(i))
                return i;
        }

        return -1;
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

    private void UnlockHero(int heroIndex)
    {
        if (heroIndex < FirstPaidHeroIndex || heroIndex > LastPaidHeroIndex)
            return;

        bool wasUnlocked = IsHeroUnlocked(heroIndex);

        PlayerPrefs.SetInt(GetHeroUnlockedKey(heroIndex), 1);
        PlayerPrefs.Save();

        if (!wasUnlocked)
        {
            Debug.Log("Hero unlocked: " + heroIndex);
            OnHeroUnlockedEvent?.Invoke();
        }
    }
}
