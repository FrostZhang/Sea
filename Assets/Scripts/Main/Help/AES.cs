
using System.IO;
using System.Security.Cryptography;
using System.Text;

// Copyright (C)  All Rights Reserved.
// Detail:AES  Red  //
// Modify:    Version:..
public static class AesSecret
{
    #region ��Կ��
    private const string saltString = "Wolfy@home";
    private const string pWDString = "home@Wolfy";
    #endregion
    #region ��/�����㷨
    /// <summary>
    /// ����
    /// </summary>
    /// <param name="sSource">��Ҫ���ܵ�����</param>
    /// <returns></returns>
    public static byte[] DecryptString(string strSource)
    {
        byte[] encryptBytes = System.Convert.FromBase64String(strSource);
        byte[] salt = Encoding.UTF8.GetBytes(saltString);
        //�ṩ�߼����ܱ�׼ (AES) �Գ��㷨���й�ʵ�֡�
        AesManaged aes = new AesManaged();
        //ͨ��ʹ�û��� System.Security.Cryptography.HMACSHA ��α�������������ʵ�ֻ����������Կ�������� (PBKDF)��
        Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(pWDString, salt);
        // ��ȡ�����ü��ܲ����Ŀ��С����λΪ��λ����
        aes.BlockSize = aes.LegalBlockSizes[0].MaxSize;
        //��ȡ���������ڶԳ��㷨����Կ��С����λΪ��λ����
        aes.KeySize = aes.LegalKeySizes[0].MaxSize;
        //��ȡ���������ڶԳ��㷨����Կ��
        aes.Key = rfc.GetBytes(aes.KeySize / 8);
        //��ȡ���������ڶԳ��㷨�ĳ�ʼ������ (IV)��
        aes.IV = rfc.GetBytes(aes.BlockSize / 8);
        // �õ�ǰ�� Key ���Ժͳ�ʼ������ IV �����Գƽ���������
        ICryptoTransform decryptTransform = aes.CreateDecryptor();
        // ���ܺ�������
        MemoryStream decryptStream = new MemoryStream();
        // �����ܺ��Ŀ������decryptStream�������ת����decryptTransform��������
        CryptoStream decryptor = new CryptoStream(
            decryptStream, decryptTransform, CryptoStreamMode.Write);
        // ��һ���ֽ�����д�뵱ǰ CryptoStream ����ɽ��ܵĹ��̣�
        decryptor.Write(encryptBytes, 0, encryptBytes.Length);
        decryptor.Close();
        // �����ܺ����õ�����ת��Ϊ�ַ���
        return decryptStream.ToArray();
    }

    /// <summary>
    /// ����
    /// </summary>
    /// <param name="sSource">��Ҫ���ܵ�����</param>
    /// <returns></returns>
    public static byte[] EncryptString(string strSource)
    {
        byte[] data = Encoding.UTF8.GetBytes(strSource);
        byte[] salt = Encoding.UTF8.GetBytes(saltString);
        // AesManaged - �߼����ܱ�׼(AES) �Գ��㷨�Ĺ�����
        AesManaged aes = new AesManaged();
        // RfcDeriveBytes - ͨ��ʹ�û��� HMACSHA ��α�������������ʵ�ֻ����������Կ�������� (PBKDF - һ�ֻ����������Կ��������)
        // ͨ�� ���� �� salt ������Կ
        Rfc2898DeriveBytes rfc = new Rfc2898DeriveBytes(pWDString, salt);

        /*
        * AesManaged.BlockSize - ���ܲ����Ŀ��С����λ��bit��
        * AesManaged.LegalBlockSizes - �Գ��㷨֧�ֵĿ��С����λ��bit��
         * AesManaged.KeySize - �Գ��㷨����Կ��С����λ��bit��
         * AesManaged.LegalKeySizes - �Գ��㷨֧�ֵ���Կ��С����λ��bit��
        * AesManaged.Key - �Գ��㷨����Կ
         * AesManaged.IV - �Գ��㷨����Կ��С
        * RfcDeriveBytes.GetBytes(int ��Ҫ���ɵ�α�����Կ�ֽ���) - ������Կ
         */
        aes.BlockSize = aes.LegalBlockSizes[0].MaxSize;
        aes.KeySize = aes.LegalKeySizes[0].MaxSize;
        aes.Key = rfc.GetBytes(aes.KeySize / 8);
        aes.IV = rfc.GetBytes(aes.BlockSize / 8);
        // �õ�ǰ�� Key ���Ժͳ�ʼ������ IV �����ԳƼ���������
        ICryptoTransform encryptTransform = aes.CreateEncryptor();
        // ���ܺ�������
        MemoryStream encryptStream = new MemoryStream();
        // �����ܺ��Ŀ������encryptStream�������ת����encryptTransform��������
        CryptoStream encryptor = new CryptoStream
           (encryptStream, encryptTransform, CryptoStreamMode.Write);
        // ��һ���ֽ�����д�뵱ǰ CryptoStream ����ɼ��ܵĹ��̣�
        encryptor.Write(data, 0, data.Length);
        encryptor.Close();
        return encryptStream.ToArray();
    }
    #endregion
}

public static class AESEazy
{
    public static void Test()
    {
        string str = "�٣����Ư��";
        string result = AesEncrypt(str, "12345678876543211234567887654abc");
        UnityEngine.MonoBehaviour.print(result);
        UnityEngine.MonoBehaviour.print(AesDecrypt(result, "12345678876543211234567887654abc"));
    }

    public static string AesEncrypt(string str, string key)
    {
        if (string.IsNullOrEmpty(str)) return null;
        byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);
        RijndaelManaged rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateEncryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return System.Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }

    /// <summary>
    /// ///  AES ����
    /// </summary>
    /// <param name="str">���ģ������ܣ�</param>
    /// <param name="key">����</param>
    /// <returns></returns>
    public static string AesDecrypt(string str, string key)
    {
        if (string.IsNullOrEmpty(str)) return null;
        byte[] toEncryptArray = System.Convert.FromBase64String(str);
        RijndaelManaged rm = new RijndaelManaged
        {
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateDecryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Encoding.UTF8.GetString(resultArray);
    }
}

/// <summary>
/// AES���ܽ���
/// </summary>
public static class AesCrypto
{
    //�ԳƼ��ܺͷ�������е�����ģʽ(ECB��CBC��CFB��OFB),�����ֵ�������Ҫ��������Կ�ĳ��ȣ�λ��Կ=λ��λ��Կ=λ��λ��Կ=λ��
    //����ο���http://www.cnblogs.com/happyhippy/archive////.html

    /// <summary>
    /// ������Կ�Ƿ���Ч���ȡ�||��
    /// </summary>
    /// <param name="key">��Կ</param>
    /// <returns>bool</returns>
    private static bool CheckKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;
        if (16.Equals(key.Length) || 24.Equals(key.Length) || 32.Equals(key.Length))
            return true;
        else
            return false;
    }

    /// <summary>
    /// ���������Ƿ���Ч���ȡ���
    /// </summary>
    /// <param name="iv">����</param>
    /// <returns>bool</returns>
    private static bool CheckIv(string iv)
    {
        if (string.IsNullOrWhiteSpace(iv))
            return false;
        if (16.Equals(iv.Length))
            return true;
        else
            return false;
    }

    #region ������string���͵�
    /// <summary>
    ///  ���� ������string
    /// </summary>
    /// <param name="palinData">����</param>
    /// <param name="key">��Կ</param>
    /// <param name="iv">����</param>
    /// <param name="encodingType">���뷽ʽ</param>
    /// <returns>string������</returns>
    public static string Encrypt(string palinData, string key, string iv)
    {
        if (string.IsNullOrWhiteSpace(palinData)) return null;
        if (!(CheckKey(key) && CheckIv(iv))) return palinData;
        byte[] toEncryptArray = Encoding.UTF8.GetBytes(palinData);
        var rm = new RijndaelManaged
        {
            IV = Encoding.UTF8.GetBytes(iv),
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateEncryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return System.Convert.ToBase64String(resultArray, 0, resultArray.Length);
    }

    /// <summary>
    ///  ���� ������string
    /// </summary>
    /// <param name="encryptedData">����</param>
    /// <param name="key">��Կ</param>
    /// <param name="iv">����</param>
    /// <param name="encodingType">���뷽ʽ</param>
    /// <returns>string������</returns>
    public static string Decrypt(string encryptedData, string key, string iv)
    {
        if (string.IsNullOrWhiteSpace(encryptedData)) return null;
        if (!(CheckKey(key) && CheckIv(iv))) return encryptedData;
        byte[] toEncryptArray = System.Convert.FromBase64String(encryptedData);
        var rm = new RijndaelManaged
        {
            IV = Encoding.UTF8.GetBytes(iv),
            Key = Encoding.UTF8.GetBytes(key),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        ICryptoTransform cTransform = rm.CreateDecryptor();
        byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        return Encoding.UTF8.GetString(resultArray);
    }
    #endregion

    #region ������byte[]���͵�
    /// <summary>  
    /// ���� ������byte[] 
    /// </summary>  
    /// <param name="palinData">����</param>  
    /// <param name="key">��Կ</param>  
    /// <param name="iv">����</param>  
    /// <returns>����</returns>  
    public static byte[] Encrypt(byte[] palinData, string key, string iv)
    {
        if (palinData == null) return null;
        if (!(CheckKey(key) && CheckIv(iv))) return palinData;
        byte[] bKey = new byte[32];
        System.Array.Copy(Encoding.UTF8.GetBytes(key.PadRight(bKey.Length)), bKey, bKey.Length);
        byte[] bVector = new byte[16];
        System.Array.Copy(Encoding.UTF8.GetBytes(iv.PadRight(bVector.Length)), bVector, bVector.Length);
        byte[] cryptograph = null; // ���ܺ������  
        Rijndael Aes = Rijndael.Create();
        // ����һ���ڴ���  
        using (MemoryStream Memory = new MemoryStream())
        {
            // ���ڴ��������װ�ɼ���������  
            using (CryptoStream Encryptor = new CryptoStream(Memory, Aes.CreateEncryptor(bKey, bVector), CryptoStreamMode.Write))
            {
                // ��������д�������  
                Encryptor.Write(palinData, 0, palinData.Length);
                Encryptor.FlushFinalBlock();
                cryptograph = Memory.ToArray();
            }
        }
        return cryptograph;
    }

    /// <summary>  
    /// ����  ������byte[] 
    /// </summary>  
    /// <param name="encryptedData">�����ܵ�����</param>  
    /// <param name="key">��Կ</param>  
    /// <param name="iv">����</param>  
    /// <returns>����</returns>  
    public static byte[] Decrypt(byte[] encryptedData, string key, string iv)
    {
        if (encryptedData == null) return null;
        if (!(CheckKey(key) && CheckIv(iv))) return encryptedData;
        byte[] bKey = new byte[32];
        System.Array.Copy(Encoding.UTF8.GetBytes(key.PadRight(bKey.Length)), bKey, bKey.Length);
        byte[] bVector = new byte[16];
        System.Array.Copy(Encoding.UTF8.GetBytes(iv.PadRight(bVector.Length)), bVector, bVector.Length);
        byte[] original = null; // ���ܺ������  
        Rijndael Aes = Rijndael.Create();
        // ����һ���ڴ������洢����  
        using (MemoryStream Memory = new MemoryStream(encryptedData))
        {
            // ���ڴ��������װ�ɼ���������  
            using (CryptoStream Decryptor = new CryptoStream(Memory, Aes.CreateDecryptor(bKey, bVector), CryptoStreamMode.Read))
            {
                // ���Ĵ洢��  
                using (MemoryStream originalMemory = new MemoryStream())
                {
                    byte[] Buffer = new byte[1024];
                    int readBytes = 0;
                    while ((readBytes = Decryptor.Read(Buffer, 0, Buffer.Length)) > 0)
                    {
                        originalMemory.Write(Buffer, 0, readBytes);
                    }
                    original = originalMemory.ToArray();
                }
            }
        }
        return original;
    }
    #endregion
}