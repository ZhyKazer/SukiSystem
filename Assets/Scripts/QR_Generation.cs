using TMPro;
using UnityEngine.UI;
using UnityEngine;
using ZXing.QrCode;
using ZXing;

public class QR_Generation : MonoBehaviour
{
    public RawImage qrImage;
    public TextMeshProUGUI bugTest;

    private void Start()
    {
        // Generate the QR code using the stored customerID from the DataHolder
        string customerID = DataHolder.customerID;
        if (!string.IsNullOrEmpty(customerID))
        {
            GenerateQRCode(customerID);
            Debug.Log("QR Code Generated for Customer ID: " + customerID);

            if (bugTest != null)
            {
                bugTest.text = "QR Code Generated for Customer ID: " + customerID;
            }
        }
        else
        {
            Debug.LogError("No Customer ID found.");
        }
    }

    public void GenerateQRCode(string textToEncode)
    {
        try
        {
            var writer = new QRCodeWriter();
            var encoded = writer.encode(textToEncode, BarcodeFormat.QR_CODE, 256, 256);

            Texture2D texture = new Texture2D(256, 256);
            for (int y = 0; y < encoded.Height; y++)
            {
                for (int x = 0; x < encoded.Width; x++)
                {
                    Color32 customDarkColor = new Color32(27, 27, 27, 255);
                    texture.SetPixel(x, y, encoded[x, y] ? Color.white : customDarkColor);
                }
            }
            texture.Apply();

            if (qrImage != null)
            {
                qrImage.texture = texture;
                qrImage.rectTransform.sizeDelta = new Vector2(256, 256);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error generating QR code: " + ex.Message);
            if (bugTest != null)
            {
                bugTest.text = "Error generating QR code: " + ex.Message;
            }
        }
    }
}
