using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using My.XXX.Shared.Common;
using Newtonsoft.Json;
using System;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Extensions for configuring data protection using an <see cref="IDataProtectionBuilder"/>.
    /// </summary>
    public static class DataProtectionBuilderExtensions
    {
        /// <summary>
        /// Configures keys to be encrypted with AES before being persisted to
        /// storage.
        /// </summary>
        /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
        /// use on the local machine, 'false' if the key should only be decryptable by the current
        /// Windows user account.
        /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
        public static IDataProtectionBuilder ProtectKeysWithAES(this IDataProtectionBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
            {
                return new ConfigureOptions<KeyManagementOptions>(options =>
                {
                    options.XmlEncryptor = new AesXmlEncryptor();
                });
            });

            return builder;
        }
    }

    /// <summary>
    /// An <see cref="IXmlEncryptor"/> that encrypts XML elements with a Aes encryptor.
    /// </summary>
    internal sealed class AesXmlEncryptor : IXmlEncryptor
    {
        /// <summary>
        /// Encrypts the specified <see cref="XElement"/> with a null encryptor, i.e.,
        /// by returning the original value of <paramref name="plaintextElement"/> unencrypted.
        /// </summary>
        /// <param name="plaintextElement">The plaintext to echo back.</param>
        /// <returns>
        /// An <see cref="EncryptedXmlInfo"/> that contains the null-encrypted value of
        /// <paramref name="plaintextElement"/> along with information about how to
        /// decrypt it.
        /// </returns>
        public EncryptedXmlInfo Encrypt(XElement plaintextElement)
        {
            ArgumentNullException.ThrowIfNull(plaintextElement);

            var Jsonxmlstr = JsonConvert.SerializeObject(plaintextElement);
            var EncryptedData = AESHelper.Encrypt(Jsonxmlstr);
            var newElement = new XElement("encryptedKey",
                new XComment(" This key is encrypted with AES."),
                new XElement("value", EncryptedData));

            return new EncryptedXmlInfo(newElement, typeof(AesXmlDecryptor));
        }
    }

    /// <summary>
    /// An <see cref="IXmlDecryptor"/> that decrypts XML elements with a Aes decryptor.
    /// </summary>
    internal sealed class AesXmlDecryptor : IXmlDecryptor
    {
        /// <summary>
        /// Decrypts the specified XML element.
        /// </summary>
        /// <param name="encryptedElement">An encrypted XML element.</param>
        /// <returns>The decrypted form of <paramref name="encryptedElement"/>.</returns>
        public XElement Decrypt(XElement encryptedElement)
        {
            ArgumentNullException.ThrowIfNull(encryptedElement);

            var EncryptedData = (string)encryptedElement.Element("value");
            var Jsonxmlstr = AESHelper.Decrypt(EncryptedData);

            return JsonConvert.DeserializeObject<XElement>(Jsonxmlstr);
        }
    }
}
