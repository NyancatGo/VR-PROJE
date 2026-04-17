using UnityEngine;
using System.Collections.Generic;

public class DepremSahnesiYoneticisi : MonoBehaviour
{
    [Header("Enkaz Prefabları")]
    [Tooltip("Büyük ve küçük enkaz prefabları")]
    public GameObject[] enkazPrefabs;
    
    [Header("Yıkık Bina Prefabları")]
    [Tooltip("DBK modüler bina parçaları")]
    public GameObject[] yikikBinaPrefabs;
    
    [Header("Yerleştirme Ayarları")]
    [Tooltip("Enkaz ve yıkık binaların yerleştirileceği yarıçap")]
    public float spawnRadius = 50f;
    
    [Tooltip("Kaç adet enkaz yerleştirilecek")]
    public int enkazSayisi = 30;
    
    [Tooltip("Kaç adet yıkık bina parçası yerleştirilecek")]
    public int yikikBinaSayisi = 15;
    
    [Header("Rastgele Dağılım")]
    [Tooltip("Enkaz grupları mı oluşturulsun?")]
    public bool gruplarHalinde = true;
    
    [Tooltip("Her grupta kaç enkaz olacak")]
    public int grupBasinaEnkaz = 5;
    
    System.Collections.IEnumerator Start()
    {
        // Onemli Fix: Eger sahnede zaten uretilmis ve kaydedilmis enkaz/bina varsa, tekrar uretme (Asiri kasmayi engeller)
        if (Application.isPlaying && transform.childCount > 0)
        {
            yield break;
        }

        if (enkazPrefabs.Length > 0)
        {
            yield return StartCoroutine(EnkazYerlestirRoutine());
        }
        
        if (yikikBinaPrefabs.Length > 0)
        {
            yield return StartCoroutine(YikikBinalarYerlestirRoutine());
        }
    }
    
    System.Collections.IEnumerator EnkazYerlestirRoutine()
    {
        if (gruplarHalinde)
        {
            int grupSayisi = enkazSayisi / grupBasinaEnkaz;
            
            for (int g = 0; g < grupSayisi; g++)
            {
                // Grup merkezi belirle
                Vector3 grupMerkezi = RandomKonumAl();
                
                for (int i = 0; i < grupBasinaEnkaz; i++)
                {
                    // Grup merkezi etrafında enkaz yerleştir
                    Vector3 offset = new Vector3(
                        Random.Range(-8f, 8f),
                        0,
                        Random.Range(-8f, 8f)
                    );
                    
                    Vector3 konum = grupMerkezi + offset;
                    konum = TerrainYuksekligineAyarla(konum);
                    
                    GameObject enkaz = Instantiate(
                        enkazPrefabs[Random.Range(0, enkazPrefabs.Length)],
                        konum,
                        Quaternion.Euler(0, Random.Range(0, 360), 0),
                        transform
                    );
                    
                    enkaz.name = $"Enkaz_Grup{g}_{i}";
                    if (i % 3 == 0) yield return null;
                }
            }
        }
        else
        {
            for (int i = 0; i < enkazSayisi; i++)
            {
                Vector3 konum = RandomKonumAl();
                konum = TerrainYuksekligineAyarla(konum);
                
                GameObject enkaz = Instantiate(
                    enkazPrefabs[Random.Range(0, enkazPrefabs.Length)],
                    konum,
                    Quaternion.Euler(0, Random.Range(0, 360), 0),
                    transform
                );
                
                enkaz.name = $"Enkaz_{i}";
                if (i % 3 == 0) yield return null;
            }
        }
    }
    
    System.Collections.IEnumerator YikikBinalarYerlestirRoutine()
    {
        for (int i = 0; i < yikikBinaSayisi; i++)
        {
            Vector3 konum = RandomKonumAl();
            konum = TerrainYuksekligineAyarla(konum);
            
            // Bazı bina parçaları biraz eğik yerleştirilsin
            Quaternion rotation = Quaternion.Euler(
                Random.Range(-5f, 15f),
                Random.Range(0, 360),
                Random.Range(-5f, 5f)
            );
            
            GameObject binaParcasi = Instantiate(
                yikikBinaPrefabs[Random.Range(0, yikikBinaPrefabs.Length)],
                konum,
                rotation,
                transform
            );
            
            binaParcasi.name = $"YikikBina_{i}";
            if (i % 2 == 0) yield return null;
        }
    }
    
    Vector3 RandomKonumAl()
    {
        float x = Random.Range(-spawnRadius, spawnRadius);
        float z = Random.Range(-spawnRadius, spawnRadius);
        return new Vector3(x, 0, z);
    }
    
    Vector3 TerrainYuksekligineAyarla(Vector3 konum)
    {
        RaycastHit hit;
        if (Physics.Raycast(konum + Vector3.up * 100, Vector3.down, out hit, 200f))
        {
            konum.y = hit.point.y;
        }
        return konum;
    }
    
    // Editor'den çağrılabilir
    [ContextMenu("Enkaz Temizle")]
    public void EnkaziTemizle()
    {
        // Sahne üzerindeki tüm enkaz ve yıkık binaları temizle
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    
    [ContextMenu("Yeniden Oluştur")]
    public void YenidenOlustur()
    {
        EnkaziTemizle();
        if (Application.isPlaying) StartCoroutine(Start());
    }
}
