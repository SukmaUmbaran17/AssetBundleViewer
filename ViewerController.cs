using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System;

public class ViewerController : MonoBehaviour
{
    private uidoc _uiDoc;
    private VisualElement _root;
    private Button _openButton; // Tombol bawaan PC akan kita sembunyikan otomatis di Android
    private Label _filePathLabel;
    
    // Variabel untuk menyimpan target objek yang di-load
    private GameObject _loadedObject;
    private string _targetFilePath = "";

    void Awake()
    {
        _uiDoc = GetComponent<UIDocument>();
        if (_uiDoc != null)
        {
            _root = _uiDoc.rootVisualElement;
            // Menghubungkan komponen UI bawaan dari file .uxml proyek ABV
            _openButton = _root.Q<Button>("OpenButton");
            _filePathLabel = _root.Q<Label>("FilePathLabel");
        }
    }

    void Start()
    {
        // JIKA BERJALAN DI ANDROID HP
        #if UNITY_ANDROID && !UNITY_EDITOR
        // Sembunyikan tombol "Open File" bawaan PC agar UI bersih
        if (_openButton != null) _openButton.style.display = DisplayStyle.None;

        try 
        {
            // 1. Ambil object Intent dari aktivitas Android Native (Tools APK kamu)
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");
            
            // 2. Ambil path string rahasia "filePath" yang dititipkan Android Native
            _targetFilePath = intent.Call<string>("getStringExtra", "filePath");
        }
        catch (Exception e)
        {
            if (_filePathLabel != null) _filePathLabel.text = "Android Intent Error: " + e.Message;
        }

        // 3. Jika path ditemukan dan valid, langsung picu fungsi load otomatis
        if (!string.IsNullOrEmpty(_targetFilePath) && File.Exists(_targetFilePath))
        {
            if (_filePathLabel != null) _filePathLabel.text = "Loading: " + Path.GetFileName(_targetFilePath);
            LoadAssetBundleFromAndroid(_targetFilePath);
        }
        else
        {
            if (_filePathLabel != null) _filePathLabel.text = "Menunggu file kiriman dari aplikasi Tools...";
        }

        // JIKA SEDANG TESTING DI DALAM UNITY EDITOR (PC)
        #else
        if (_openButton != null)
        {
            // Tetap izinkan tombol bawaan berfungsi di Editor PC untuk kebutuhan debug kamu
            _openButton.clicked += OnOpenButtonClickedEditor;
        }
        #endif
    }

    // Fungsi Pengganti: Khusus mengeksekusi file bundle kiriman sistem Android Native
    private void LoadAssetBundleFromAndroid(string path)
    {
        // Bersihkan objek lama jika sebelumnya sudah pernah me-load sesuatu
        if (_loadedObject != null) Destroy(_loadedObject);

        // Load AssetBundle dari penyimpanan lokal Android secara aman
        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            if (_filePathLabel != null) _filePathLabel.text = "Gagal memproses struktur file .unity3d";
            return;
        }

        // Ambil aset internal pertama yang berwujud GameObject (VFX / Animasi / Mesh)
        string[] assetNames = bundle.GetAllAssetNames();
        if (assetNames.Length > 0)
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(assetNames[0]);
            if (prefab != null)
            {
                // Munculkan objek tepat di tengah radar kamera viewer
                _loadedObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                if (_filePathLabel != null) _filePathLabel.text = "Sukses Menampilkan: " + prefab.name;
            }
            else
            {
                if (_filePathLabel != null) _filePathLabel.text = "File berhasil dibaca, tapi tidak ditemukan objek 3D/VFX di dalamnya.";
            }
        }
        
        // Unload memori bundle mentah agar RAM HP tidak sesak, pertahankan objek yang sudah di-instantiate
        bundle.Unload(false);
    }

    // Fungsi Cadangan: Hanya berjalan jika kamu membuka proyek ini di Unity Editor PC (Bukan Android)
    private void OnOpenButtonClickedEditor()
    {
        // Catatan: Jika dicoba di Editor PC, kamu bisa melakukan simulasi manual atau memasukkan dummy path.
        Debug.Log("Tombol Open diklik di Editor Mode.");
    }
}
