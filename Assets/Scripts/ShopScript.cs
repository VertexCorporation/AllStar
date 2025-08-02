using UnityEngine;
using TMPro;
using System;
using UnityEngine.Purchasing;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

[Serializable]
public class ConsumableItem
{
    public string Name;
    public string Id;
    public string desc;
    public float price;
}

public class ShopScript : MonoBehaviour, IDetailedStoreListener
{
    IStoreController m_StoreContoller;

    public ConsumableItem three;
    public ConsumableItem five;
    public ConsumableItem ten;
    public ConsumableItem tf;
    public ConsumableItem f;
    public ConsumableItem ohtl;
    public ConsumableItem thf;
    public ConsumableItem fh;
    public ConsumableItem ot;

    public TMP_InputField inp;


    public Data data;
    public Payload payload;
    public PayloadData payloadData;
    public ScoreManager sm;
    private void Start()
    {
        SetupBuilder();
        sm = FindFirstObjectByType<ScoreManager>();
    }

    #region setup and initialize
    void SetupBuilder()
    {

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct(three.Id, ProductType.Consumable);
        builder.AddProduct(five.Id, ProductType.Consumable);
        builder.AddProduct(ten.Id, ProductType.Consumable);
        builder.AddProduct(tf.Id, ProductType.Consumable);
        builder.AddProduct(f.Id, ProductType.Consumable);
        builder.AddProduct(ohtl.Id, ProductType.Consumable);
        builder.AddProduct(thf.Id, ProductType.Consumable);
        builder.AddProduct(fh.Id, ProductType.Consumable);
        builder.AddProduct(ot.Id, ProductType.Consumable);

        UnityPurchasing.Initialize(this, builder);
    }
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        print("Success");
        m_StoreContoller = controller;
    }
    #endregion


    #region button clicks 
    public void threeb()
    {
        m_StoreContoller.InitiatePurchase(three.Id);
    }
    public void fiveb()
    {
        m_StoreContoller.InitiatePurchase(five.Id);
    }
    public void tenb()
    {
        m_StoreContoller.InitiatePurchase(ten.Id);
    }
    public void tfb()
    {
        m_StoreContoller.InitiatePurchase(tf.Id);
    }
    public void fb()
    {
        m_StoreContoller.InitiatePurchase(f.Id);
    }
    public void ohtlb()
    {
        m_StoreContoller.InitiatePurchase(ohtl.Id);
    }
    public void thfb()
    {
        m_StoreContoller.InitiatePurchase(thf.Id);
    }
    public void fhb()
    {
        m_StoreContoller.InitiatePurchase(fh.Id);
    }
    public void otb()
    {
        m_StoreContoller.InitiatePurchase(ot.Id);
    }
    #endregion


    #region main

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        var product = purchaseEvent.purchasedProduct;

        Debug.Log($"[ShopScript] Purchase process started for product: {product.definition.id}");

#if UNITY_EDITOR
        // --- LOGIC FOR THE UNITY EDITOR ---
        // When running in the Editor, the "FakeStore" is used. Its receipt is invalid and cannot be parsed.
        // We will skip receipt validation entirely and consider the purchase successful for testing purposes.
        Debug.Log($"[ShopScript] Purchase successful in Editor (FakeStore) for donation item: {product.definition.id}. Thank you for your support!");
        // No rewards are granted as this is a donation.

#else
        // --- LOGIC FOR A REAL DEVICE (ANDROID/IOS) ---
        // On a real device, we should validate the receipt from Google Play or the App Store
        // to ensure the purchase is legitimate.
        // The most secure method is to send the receipt to your own server for validation.
        // For now, we will perform a basic client-side check to see if the receipt can be parsed.
        try
        {
            // We attempt to parse the receipt. If this succeeds, it's a basic form of validation.
            // If it fails, the catch block will handle it.
            Data data = JsonUtility.FromJson<Data>(product.receipt);
            Payload payload = JsonUtility.FromJson<Payload>(data.Payload);

            Debug.Log($"[ShopScript] Receipt for donation item {product.definition.id} was parsed successfully on device. Thank you for your support!");
            // No rewards are granted as this is a donation.
        }
        catch (Exception ex)
        {
            // If the receipt is invalid or cannot be parsed, this block will execute.
            Debug.LogError($"[ShopScript] CRITICAL: Invalid receipt for donation item {product.definition.id}. The purchase may be fraudulent. Error: {ex.Message}");
            // It is important to NOT grant anything here and to log this event.
        }
#endif

        // Mark the transaction as complete.
        return PurchaseProcessingResult.Complete;
    }
    #endregion


    #region error handeling
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        print("failed" + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        print("initialize failed" + error + message);
    }



    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        print("purchase failed" + failureReason);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        print("purchase failed" + failureDescription);
    }
    #endregion


    #region extra 

    void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        // This method is part of the IDetailedStoreListener interface.
        // We already handle purchase failures in the other OnPurchaseFailed method,
        // so we can log more detailed information here or leave it empty.
        Debug.LogError($"[ShopScript] Detailed Purchase Failed. Product: {product.definition.id}, Reason: {failureDescription.reason}, Message: {failureDescription.message}");
    }



    #endregion

}


[Serializable]
public class SkuDetails
{
    public string productId;
    public string type;
    public string title;
    public string name;
    public string iconUrl;
    public string description;
    public string price;
    public long price_amount_micros;
    public string price_currency_code;
    public string skuDetailsToken;
}

[Serializable]
public class PayloadData
{
    public string orderId;
    public string packageName;
    public string productId;
    public long purchaseTime;
    public int purchaseState;
    public string purchaseToken;
    public int quantity;
    public bool acknowledged;
}

[Serializable]
public class Payload
{
    public string json;
    public string signature;
    public List<SkuDetails> skuDetails;
    public PayloadData payloadData;
}

[Serializable]
public class Data
{
    public string Payload;
    public string Store;
    public string TransactionID;
}