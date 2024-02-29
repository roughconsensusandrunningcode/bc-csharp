﻿using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.Test;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Org.BouncyCastle.Bcpg.OpenPgp.Tests
{
    [TestFixture]
    public class PgpCryptoRefreshTest
        : SimpleTest
    {
        public override string Name => "PgpCryptoRefreshTest";


        // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v4-ed25519legacy-key
        private readonly byte[] v4Ed25519LegacyPubkeySample = Base64.Decode(
            "xjMEU/NfCxYJKwYBBAHaRw8BAQdAPwmJlL3ZFu1AUxl5NOSofIBzOhKA1i+AEJku" +
            "Q+47JAY=");

        // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v4-ed25519legacy-sig
        private readonly byte[] v4Ed25519LegacySignatureSample = Base64.Decode(
            "iF4EABYIAAYFAlX5X5UACgkQjP3hIZeWWpr2IgD/VvkMypjiECY3vZg/2xbBMd/S" +
            "ftgr9N3lYG4NdWrtM2YBANCcT6EVJ/A44PV/IgHYLy6iyQMyZfps60iehUuuYbQE");

        // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v6-certificate-trans
        private readonly byte[] v6Certificate = Base64.Decode(
            "xioGY4d/4xsAAAAg+U2nu0jWCmHlZ3BqZYfQMxmZu52JGggkLq2EVD34laPCsQYf" +
            "GwoAAABCBYJjh3/jAwsJBwUVCg4IDAIWAAKbAwIeCSIhBssYbE8GCaaX5NUt+mxy" +
            "KwwfHifBilZwj2Ul7Ce62azJBScJAgcCAAAAAK0oIBA+LX0ifsDm185Ecds2v8lw" +
            "gyU2kCcUmKfvBXbAf6rhRYWzuQOwEn7E/aLwIwRaLsdry0+VcallHhSu4RN6HWaE" +
            "QsiPlR4zxP/TP7mhfVEe7XWPxtnMUMtf15OyA51YBM4qBmOHf+MZAAAAIIaTJINn" +
            "+eUBXbki+PSAld2nhJh/LVmFsS+60WyvXkQ1wpsGGBsKAAAALAWCY4d/4wKbDCIh" +
            "BssYbE8GCaaX5NUt+mxyKwwfHifBilZwj2Ul7Ce62azJAAAAAAQBIKbpGG2dWTX8" +
            "j+VjFM21J0hqWlEg+bdiojWnKfA5AQpWUWtnNwDEM0g12vYxoWM8Y81W+bHBw805" +
            "I8kWVkXU6vFOi+HWvv/ira7ofJu16NnoUkhclkUrk0mXubZvyl4GBg==");

        // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v6-secret-key-transf
        private readonly byte[] v6UnlockedSecretKey = Base64.Decode(
            "xUsGY4d/4xsAAAAg+U2nu0jWCmHlZ3BqZYfQMxmZu52JGggkLq2EVD34laMAGXKB" +
            "exK+cH6NX1hs5hNhIB00TrJmosgv3mg1ditlsLfCsQYfGwoAAABCBYJjh3/jAwsJ" +
            "BwUVCg4IDAIWAAKbAwIeCSIhBssYbE8GCaaX5NUt+mxyKwwfHifBilZwj2Ul7Ce6" +
            "2azJBScJAgcCAAAAAK0oIBA+LX0ifsDm185Ecds2v8lwgyU2kCcUmKfvBXbAf6rh" +
            "RYWzuQOwEn7E/aLwIwRaLsdry0+VcallHhSu4RN6HWaEQsiPlR4zxP/TP7mhfVEe" +
            "7XWPxtnMUMtf15OyA51YBMdLBmOHf+MZAAAAIIaTJINn+eUBXbki+PSAld2nhJh/" +
            "LVmFsS+60WyvXkQ1AE1gCk95TUR3XFeibg/u/tVY6a//1q0NWC1X+yui3O24wpsG" +
            "GBsKAAAALAWCY4d/4wKbDCIhBssYbE8GCaaX5NUt+mxyKwwfHifBilZwj2Ul7Ce6" +
            "2azJAAAAAAQBIKbpGG2dWTX8j+VjFM21J0hqWlEg+bdiojWnKfA5AQpWUWtnNwDE" +
            "M0g12vYxoWM8Y81W+bHBw805I8kWVkXU6vFOi+HWvv/ira7ofJu16NnoUkhclkUr" +
            "k0mXubZvyl4GBg==");

        // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-locked-v6-secret-key
        private readonly byte[] v6LockedSecretKey = Base64.Decode(
            "xYIGY4d/4xsAAAAg+U2nu0jWCmHlZ3BqZYfQMxmZu52JGggkLq2EVD34laP9JgkC" +
            "FARdb9ccngltHraRe25uHuyuAQQVtKipJ0+r5jL4dacGWSAheCWPpITYiyfyIOPS" +
            "3gIDyg8f7strd1OB4+LZsUhcIjOMpVHgmiY/IutJkulneoBYwrEGHxsKAAAAQgWC" +
            "Y4d/4wMLCQcFFQoOCAwCFgACmwMCHgkiIQbLGGxPBgmml+TVLfpscisMHx4nwYpW" +
            "cI9lJewnutmsyQUnCQIHAgAAAACtKCAQPi19In7A5tfORHHbNr/JcIMlNpAnFJin" +
            "7wV2wH+q4UWFs7kDsBJ+xP2i8CMEWi7Ha8tPlXGpZR4UruETeh1mhELIj5UeM8T/" +
            "0z+5oX1RHu11j8bZzFDLX9eTsgOdWATHggZjh3/jGQAAACCGkySDZ/nlAV25Ivj0" +
            "gJXdp4SYfy1ZhbEvutFsr15ENf0mCQIUBA5hhGgp2oaavg6mFUXcFMwBBBUuE8qf" +
            "9Ock+xwusd+GAglBr5LVyr/lup3xxQvHXFSjjA2haXfoN6xUGRdDEHI6+uevKjVR" +
            "v5oAxgu7eJpaXNjCmwYYGwoAAAAsBYJjh3/jApsMIiEGyxhsTwYJppfk1S36bHIr" +
            "DB8eJ8GKVnCPZSXsJ7rZrMkAAAAABAEgpukYbZ1ZNfyP5WMUzbUnSGpaUSD5t2Ki" +
            "Nacp8DkBClZRa2c3AMQzSDXa9jGhYzxjzVb5scHDzTkjyRZWRdTq8U6L4da+/+Kt" +
            "ruh8m7Xo2ehSSFyWRSuTSZe5tm/KXgYG");

        // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-cleartext-signed-mes
        private readonly string v6SampleCleartextSignedMessage = "What we need from the grocery store:\r\n\r\n- tofu\r\n- vegetables\r\n- noodles\r\n";
        private readonly byte[] v6SampleCleartextSignedMessageSignature = Base64.Decode(
            "wpgGARsKAAAAKQWCY5ijYyIhBssYbE8GCaaX5NUt+mxyKwwfHifBilZwj2Ul7Ce6" +
            "2azJAAAAAGk2IHZJX1AhiJD39eLuPBgiUU9wUA9VHYblySHkBONKU/usJ9BvuAqo" +
            "/FvLFuGWMbKAdA+epq7V4HOtAPlBWmU8QOd6aud+aSunHQaaEJ+iTFjP2OMW0KBr" +
            "NK2ay45cX1IVAQ==");

        // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-inline-signed-messag
        private readonly byte[] v6SampleInlineSignedMessage = Base64.Decode(
            "xEYGAQobIHZJX1AhiJD39eLuPBgiUU9wUA9VHYblySHkBONKU/usyxhsTwYJppfk" +
            "1S36bHIrDB8eJ8GKVnCPZSXsJ7rZrMkBy0p1AAAAAABXaGF0IHdlIG5lZWQgZnJv" +
            "bSB0aGUgZ3JvY2VyeSBzdG9yZToKCi0gdG9mdQotIHZlZ2V0YWJsZXMKLSBub29k" +
            "bGVzCsKYBgEbCgAAACkFgmOYo2MiIQbLGGxPBgmml+TVLfpscisMHx4nwYpWcI9l" +
            "JewnutmsyQAAAABpNiB2SV9QIYiQ9/Xi7jwYIlFPcFAPVR2G5ckh5ATjSlP7rCfQ" +
            "b7gKqPxbyxbhljGygHQPnqau1eBzrQD5QVplPEDnemrnfmkrpx0GmhCfokxYz9jj" +
            "FtCgazStmsuOXF9SFQE=");

        private readonly char[] emptyPassphrase = Array.Empty<char>();

        [Test]
        public void Version4Ed25519LegacyPubkeySampleTest()
        {
            // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v4-ed25519legacy-key
            PgpPublicKeyRing pubRing = new PgpPublicKeyRing(v4Ed25519LegacyPubkeySample);
            PgpPublicKey pubKey = pubRing.GetPublicKey();

            IsEquals(pubKey.Algorithm, PublicKeyAlgorithmTag.EdDsa_Legacy);
            IsEquals(pubKey.CreationTime.ToString("yyyyMMddHHmmss"), "20140819142827");

            byte[] expectedFingerprint = Hex.Decode("C959BDBAFA32A2F89A153B678CFDE12197965A9A");
            IsEquals((ulong)pubKey.KeyId, 0x8CFDE12197965A9A);
            IsTrue("wrong fingerprint", AreEqual(pubKey.GetFingerprint(), expectedFingerprint));
        }

        [Test]
        public void Version4Ed25519LegacyCreateTest()
        {
            // create a v4 EdDsa_Legacy Pubkey with the same key material and creation datetime as the test vector
            // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v4-ed25519legacy-key
            // then check KeyId/Fingerprint
            var key = new Ed25519PublicKeyParameters(Hex.Decode("3f098994bdd916ed4053197934e4a87c80733a1280d62f8010992e43ee3b2406"));
            var pubKey = new PgpPublicKey(PublicKeyAlgorithmTag.EdDsa_Legacy, key, DateTime.Parse("2014-08-19 14:28:27Z"));
            IsEquals(pubKey.Algorithm, PublicKeyAlgorithmTag.EdDsa_Legacy);
            IsEquals(pubKey.CreationTime.ToString("yyyyMMddHHmmss"), "20140819142827");

            byte[] expectedFingerprint = Hex.Decode("C959BDBAFA32A2F89A153B678CFDE12197965A9A");
            IsEquals((ulong)pubKey.KeyId, 0x8CFDE12197965A9A);
            IsTrue("wrong fingerprint", AreEqual(pubKey.GetFingerprint(), expectedFingerprint));
        }

        [Test]
        public void Version4Ed25519LegacySignatureSampleTest()
        {
            // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v4-ed25519legacy-sig
            PgpPublicKeyRing pubRing = new PgpPublicKeyRing(v4Ed25519LegacyPubkeySample);
            PgpPublicKey pubKey = pubRing.GetPublicKey();

            PgpObjectFactory factory = new PgpObjectFactory(v4Ed25519LegacySignatureSample);
            PgpSignatureList sigList = factory.NextPgpObject() as PgpSignatureList;
            PgpSignature signature = sigList[0];

            IsEquals(signature.KeyId, pubKey.KeyId);
            IsEquals(signature.KeyAlgorithm, PublicKeyAlgorithmTag.EdDsa_Legacy);
            IsEquals(signature.HashAlgorithm, HashAlgorithmTag.Sha256);
            IsEquals(signature.CreationTime.ToString("yyyyMMddHHmmss"), "20150916122453");

            byte[] data = Encoding.UTF8.GetBytes("OpenPGP");
            VerifySignature(signature, data, pubKey);

            // test with wrong data, verification should fail
            data = Encoding.UTF8.GetBytes("OpePGP");
            VerifySignature(signature, data, pubKey, shouldFail: true);
        }

        [Test]
        public void Version6CertificateParsingTest()
        {
            /*
             * https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v6-certificate-trans
             * A Transferable Public Key consisting of:
             *     A v6 Ed25519 Public-Key packet
             *     A v6 direct key self-signature
             *     A v6 X25519 Public-Subkey packet
             *     A v6 subkey binding signature
             */
            PgpPublicKeyRing pubRing = new PgpPublicKeyRing(v6Certificate);
            PgpPublicKey[] publicKeys = pubRing.GetPublicKeys().ToArray();
            IsEquals("wrong number of public keys", publicKeys.Length, 2);

            // master key
            PgpPublicKey masterKey = publicKeys[0];
            FailIf("wrong detection of master key", !masterKey.IsMasterKey);
            IsEquals(masterKey.Algorithm, PublicKeyAlgorithmTag.Ed25519);
            IsEquals(masterKey.CreationTime.ToString("yyyyMMddHHmmss"), "20221130160803");
            byte[] expectedFingerprint = Hex.Decode("CB186C4F0609A697E4D52DFA6C722B0C1F1E27C18A56708F6525EC27BAD9ACC9");
            IsEquals((ulong)masterKey.KeyId, 0xCB186C4F0609A697);
            IsTrue("wrong master key fingerprint", AreEqual(masterKey.GetFingerprint(), expectedFingerprint));

            // Verify direct key self-signature
            PgpSignature selfSig = masterKey.GetSignatures().First();
            IsTrue(selfSig.SignatureType == PgpSignature.DirectKey);
            selfSig.InitVerify(masterKey);
            FailIf("self signature verification failed", !selfSig.VerifyCertification(masterKey));

            // subkey
            PgpPublicKey subKey = publicKeys[1];
            FailIf("wrong detection of encryption subkey", !subKey.IsEncryptionKey);
            IsEquals(subKey.Algorithm, PublicKeyAlgorithmTag.X25519);
            expectedFingerprint = Hex.Decode("12C83F1E706F6308FE151A417743A1F033790E93E9978488D1DB378DA9930885");
            IsEquals(subKey.KeyId, 0x12C83F1E706F6308);
            IsTrue("wrong sub key fingerprint", AreEqual(subKey.GetFingerprint(), expectedFingerprint));

            // Verify subkey binding signature
            PgpSignature bindingSig = subKey.GetSignatures().First();
            IsTrue(bindingSig.SignatureType == PgpSignature.SubkeyBinding);
            bindingSig.InitVerify(masterKey);
            FailIf("subkey binding signature verification failed", !bindingSig.VerifyCertification(masterKey, subKey));

            // Encode test
            using (MemoryStream ms = new MemoryStream())
            {
                using (BcpgOutputStream bs = new BcpgOutputStream(ms, newFormatOnly: true))
                {
                    pubRing.Encode(bs);
                }

                byte[] encoded = ms.ToArray();
                IsTrue(AreEqual(encoded, v6Certificate));
            }
        }

        [Test]
        public void Version6PublicKeyCreationTest()
        {
            /* 
             * Create a v6 Ed25519 pubkey with the same key material and creation datetime as the test vector
             * https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v6-certificate-trans
             * then check the fingerprint and verify a signature
            */
            byte[] keyMaterial = Hex.Decode("f94da7bb48d60a61e567706a6587d0331999bb9d891a08242ead84543df895a3");
            var key = new Ed25519PublicKeyParameters(keyMaterial);
            var pubKey = new PgpPublicKey(PublicKeyPacket.Version6, PublicKeyAlgorithmTag.Ed25519, key, DateTime.Parse("2022-11-30 16:08:03Z"));

            IsEquals(pubKey.Algorithm, PublicKeyAlgorithmTag.Ed25519);
            IsEquals(pubKey.CreationTime.ToString("yyyyMMddHHmmss"), "20221130160803");
            byte[] expectedFingerprint = Hex.Decode("CB186C4F0609A697E4D52DFA6C722B0C1F1E27C18A56708F6525EC27BAD9ACC9");
            IsEquals((ulong)pubKey.KeyId, 0xCB186C4F0609A697);
            IsTrue("wrong master key fingerprint", AreEqual(pubKey.GetFingerprint(), expectedFingerprint));

            VerifyEncodedSignature(
                v6SampleCleartextSignedMessageSignature,
                Encoding.UTF8.GetBytes(v6SampleCleartextSignedMessage),
                pubKey);

            VerifyEncodedSignature(
                v6SampleCleartextSignedMessageSignature,
                Encoding.UTF8.GetBytes("wrongdata"),
                pubKey,
                shouldFail: true);
        }

        [Test]
        public void Version6UnlockedSecretKeyParsingTest()
        {
            /*
             * https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-v6-secret-key-transf
             * A Transferable Secret Key consisting of:
             *     A v6 Ed25519 Secret-Key packet
             *     A v6 direct key self-signature
             *     A v6 X25519 Secret-Subkey packet
             *     A v6 subkey binding signature
             */

            PgpSecretKeyRing secretKeyRing = new PgpSecretKeyRing(v6UnlockedSecretKey);
            PgpSecretKey[] secretKeys = secretKeyRing.GetSecretKeys().ToArray();
            IsEquals("wrong number of secret keys", secretKeys.Length, 2);

            // signing key
            PgpSecretKey signingKey = secretKeys[0];
            IsEquals(signingKey.PublicKey.Algorithm, PublicKeyAlgorithmTag.Ed25519);
            IsEquals((ulong)signingKey.PublicKey.KeyId, 0xCB186C4F0609A697);

            // generate and verify a v6 signature
            byte[] data = Encoding.UTF8.GetBytes("OpenPGP");
            byte[] wrongData = Encoding.UTF8.GetBytes("OpePGP");
            PgpSignatureGenerator sigGen = new PgpSignatureGenerator(signingKey.PublicKey.Algorithm, HashAlgorithmTag.Sha512);
            PgpSignatureSubpacketGenerator spkGen = new PgpSignatureSubpacketGenerator();
            PgpPrivateKey privKey = signingKey.ExtractPrivateKey(emptyPassphrase);
            spkGen.SetIssuerFingerprint(false, signingKey);
            sigGen.InitSign(PgpSignature.CanonicalTextDocument, privKey, new SecureRandom());
            sigGen.Update(data);
            sigGen.SetHashedSubpackets(spkGen.Generate());
            PgpSignature signature = sigGen.Generate();

            VerifySignature(signature, data, signingKey.PublicKey);
            VerifySignature(signature, wrongData, signingKey.PublicKey, shouldFail: true);

            byte[] encodedSignature = signature.GetEncoded();
            VerifyEncodedSignature(encodedSignature, data, signingKey.PublicKey);
            VerifyEncodedSignature(encodedSignature, wrongData, signingKey.PublicKey, shouldFail: true);

            // encryption key
            PgpSecretKey encryptionKey = secretKeys[1];
            IsEquals(encryptionKey.PublicKey.Algorithm, PublicKeyAlgorithmTag.X25519);
            IsEquals(encryptionKey.PublicKey.KeyId, 0x12C83F1E706F6308);

            AsymmetricCipherKeyPair alice = GetKeyPair(encryptionKey);
            IAsymmetricCipherKeyPairGenerator kpGen = new X25519KeyPairGenerator();
            kpGen.Init(new X25519KeyGenerationParameters(new SecureRandom()));
            AsymmetricCipherKeyPair bob = kpGen.GenerateKeyPair();

            IsTrue("X25519 agreement failed", EncryptThenDecryptX25519Test(alice, bob));

            // Encode test
            using (MemoryStream ms = new MemoryStream())
            {
                using (BcpgOutputStream bs = new BcpgOutputStream(ms, newFormatOnly: true))
                {
                    secretKeyRing.Encode(bs);
                }

                byte[] encoded = ms.ToArray();
                IsTrue(AreEqual(encoded, v6UnlockedSecretKey));
            }

            // generate and verify a v6 userid self-cert
            string userId = "Alice <alice@example.com>";
            string wrongUserId = "Bob <bob@example.com>";
            sigGen.InitSign(PgpSignature.PositiveCertification, privKey, new SecureRandom());
            signature = sigGen.GenerateCertification(userId, signingKey.PublicKey);
            signature.InitVerify(signingKey.PublicKey);
            if (!signature.VerifyCertification(userId, signingKey.PublicKey))
            {
                Fail("self-cert verification failed.");
            }
            signature.InitVerify(signingKey.PublicKey);
            if (signature.VerifyCertification(wrongUserId, signingKey.PublicKey))
            {
                Fail("self-cert verification failed.");
            }
            PgpPublicKey key = PgpPublicKey.AddCertification(signingKey.PublicKey, userId, signature);
            byte[] keyEnc = key.GetEncoded();
            PgpPublicKeyRing tmpRing = new PgpPublicKeyRing(keyEnc);
            key = tmpRing.GetPublicKey();
            IsTrue(key.GetUserIds().Contains(userId));

            // generate and verify a v6 cert revocation
            sigGen.InitSign(PgpSignature.KeyRevocation, privKey, new SecureRandom());
            signature = sigGen.GenerateCertification(signingKey.PublicKey);
            signature.InitVerify(signingKey.PublicKey);
            if (!signature.VerifyCertification(signingKey.PublicKey))
            {
                Fail("revocation verification failed.");
            }
            key = PgpPublicKey.AddCertification(signingKey.PublicKey, signature);
            keyEnc = key.GetEncoded();
            tmpRing = new PgpPublicKeyRing(keyEnc);
            key = tmpRing.GetPublicKey();
            IsTrue(key.IsRevoked());
        }

        [Test]
        public void Version6LockedSecretKeyParsingTest()
        {
            /*
             * https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-locked-v6-secret-key
             * The same secret key as in Version6UnlockedSecretKeyParsingTest, but the secret key
             * material is locked with a passphrase using AEAD and Argon2.
             * 
             * AEAD/Argon passphrase decryption is not implemented yet, so we just test
             * parsing and encoding
             */

            PgpSecretKeyRing secretKeyRing = new PgpSecretKeyRing(v6LockedSecretKey);
            PgpSecretKey[] secretKeys = secretKeyRing.GetSecretKeys().ToArray();
            IsEquals("wrong number of secret keys", secretKeys.Length, 2);

            // signing key
            PgpSecretKey signingKey = secretKeys[0];
            IsEquals(signingKey.PublicKey.Algorithm, PublicKeyAlgorithmTag.Ed25519);
            IsEquals((ulong)signingKey.PublicKey.KeyId, 0xCB186C4F0609A697);

            // encryption key
            PgpSecretKey encryptionKey = secretKeys[1];
            IsEquals(encryptionKey.PublicKey.Algorithm, PublicKeyAlgorithmTag.X25519);
            IsEquals(encryptionKey.PublicKey.KeyId, 0x12C83F1E706F6308);

            // Encode test
            using (MemoryStream ms = new MemoryStream())
            {
                using (BcpgOutputStream bs = new BcpgOutputStream(ms, newFormatOnly: true))
                {
                    secretKeyRing.Encode(bs);
                }

                byte[] encoded = ms.ToArray();
                IsTrue(AreEqual(encoded, v6LockedSecretKey));
            }
        }

        [Test]
        public void Version6SampleCleartextSignedMessageVerifySignatureTest()
        {
            // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-cleartext-signed-mes

            PgpPublicKeyRing pubRing = new PgpPublicKeyRing(v6Certificate);
            PgpPublicKey pubKey = pubRing.GetPublicKey();

            VerifyEncodedSignature(
                v6SampleCleartextSignedMessageSignature,
                Encoding.UTF8.GetBytes(v6SampleCleartextSignedMessage),
                pubKey);

            VerifyEncodedSignature(
                v6SampleCleartextSignedMessageSignature,
                Encoding.UTF8.GetBytes("wrongdata"),
                pubKey,
                shouldFail: true);
        }

        [Test]
        public void Version6SampleInlineSignedMessageVerifySignatureTest()
        {
            // https://www.ietf.org/archive/id/draft-ietf-openpgp-crypto-refresh-13.html#name-sample-inline-signed-messag
            PgpPublicKeyRing pubRing = new PgpPublicKeyRing(v6Certificate);
            PgpPublicKey pubKey = pubRing.GetPublicKey();

            VerifyInlineSignature(v6SampleInlineSignedMessage, pubKey);
        }

        [Test]
        public void Version6GenerateAndVerifyInlineSignatureTest()
        {
            PgpSecretKeyRing secretKeyRing = new PgpSecretKeyRing(v6UnlockedSecretKey);
            PgpSecretKey signingKey = secretKeyRing.GetSecretKey();
            PgpPrivateKey privKey = signingKey.ExtractPrivateKey(emptyPassphrase);
            byte[] data = Encoding.UTF8.GetBytes("OpenPGP\nOpenPGP");
            byte[] inlineSignatureMessage;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BcpgOutputStream bcOut = new BcpgOutputStream(ms, newFormatOnly: true))
                {
                    PgpSignatureGenerator sGen = new PgpSignatureGenerator(signingKey.PublicKey.Algorithm, HashAlgorithmTag.Sha384);
                    sGen.InitSign(PgpSignature.CanonicalTextDocument, privKey, new SecureRandom());
                    sGen.GenerateOnePassVersion(false).Encode(bcOut);

                    PgpLiteralDataGenerator lGen = new PgpLiteralDataGenerator();
                    DateTime modificationTime = DateTime.UtcNow;
                    using (var lOut = lGen.Open(
                        new UncloseableStream(bcOut),
                        PgpLiteralData.Utf8,
                        "_CONSOLE",
                        data.Length,
                        modificationTime))
                    {
                        lOut.Write(data, 0, data.Length);
                        sGen.Update(data);
                    }

                    sGen.Generate().Encode(bcOut);
                }

                inlineSignatureMessage = ms.ToArray();
            }

            VerifyInlineSignature(inlineSignatureMessage, signingKey.PublicKey);
            // corrupt data
            inlineSignatureMessage[88] = 80;
            VerifyInlineSignature(inlineSignatureMessage, signingKey.PublicKey, shouldFail: true);
        }

        private void VerifyInlineSignature(byte[] message, PgpPublicKey signer, bool shouldFail = false)
        {
            byte[] data;
            PgpObjectFactory factory = new PgpObjectFactory(message);

            PgpOnePassSignatureList p1 = factory.NextPgpObject() as PgpOnePassSignatureList;
            PgpOnePassSignature ops = p1[0];

            PgpLiteralData p2 = factory.NextPgpObject() as PgpLiteralData;
            Stream dIn = p2.GetInputStream();

            ops.InitVerify(signer);

            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[30];
                int bytesRead;
                while ((bytesRead = dIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ops.Update(buffer, 0, bytesRead);
                    ms.Write(buffer, 0, bytesRead);
                }

                data = ms.ToArray();
            }
            PgpSignatureList p3 = factory.NextPgpObject() as PgpSignatureList;
            PgpSignature sig = p3[0];

            bool result = ops.Verify(sig) != shouldFail;
            IsTrue("signature test failed", result);

            VerifySignature(sig, data, signer, shouldFail);
        }

        private void VerifySignature(PgpSignature signature, byte[] data, PgpPublicKey signer, bool shouldFail = false)
        {
            IsEquals(signature.KeyAlgorithm, signer.Algorithm);
            // the version of the signature is bound to the version of the signing key
            IsEquals(signature.Version, signer.Version);

            if (signature.KeyId != 0)
            {
                IsEquals(signature.KeyId, signer.KeyId);
            }
            byte[] issuerFpt = signature.GetIssuerFingerprint();
            if (issuerFpt != null)
            {
                IsTrue(AreEqual(issuerFpt, signer.GetFingerprint()));
            }

            signature.InitVerify(signer);
            signature.Update(data);

            bool result = signature.Verify() != shouldFail;
            IsTrue("signature test failed", result);
        }

        private void VerifyEncodedSignature(byte[] sigPacket, byte[] data, PgpPublicKey signer, bool shouldFail = false)
        {
            PgpObjectFactory factory = new PgpObjectFactory(sigPacket);
            PgpSignatureList sigList = factory.NextPgpObject() as PgpSignatureList;
            PgpSignature signature = sigList[0];

            VerifySignature(signature, data, signer, shouldFail);
        }

        private static AsymmetricCipherKeyPair GetKeyPair(PgpSecretKey secretKey, string password = "")
        {
            return new AsymmetricCipherKeyPair(
                secretKey.PublicKey.GetKey(),
                secretKey.ExtractPrivateKey(password.ToCharArray()).Key);
        }

        private static bool EncryptThenDecryptX25519Test(AsymmetricCipherKeyPair alice, AsymmetricCipherKeyPair bob)
        {
            X25519Agreement agreeA = new X25519Agreement();
            agreeA.Init(alice.Private);
            byte[] secretA = new byte[agreeA.AgreementSize];
            agreeA.CalculateAgreement(bob.Public, secretA, 0);

            X25519Agreement agreeB = new X25519Agreement();
            agreeB.Init(bob.Private);
            byte[] secretB = new byte[agreeB.AgreementSize];
            agreeB.CalculateAgreement(alice.Public, secretB, 0);

            return Arrays.AreEqual(secretA, secretB);
        }

        public override void PerformTest()
        {
            Version4Ed25519LegacyPubkeySampleTest();
            Version4Ed25519LegacySignatureSampleTest();
            Version4Ed25519LegacyCreateTest();

            Version6CertificateParsingTest();
            Version6PublicKeyCreationTest();
            Version6UnlockedSecretKeyParsingTest();
            Version6LockedSecretKeyParsingTest();
            Version6SampleCleartextSignedMessageVerifySignatureTest();
            Version6SampleInlineSignedMessageVerifySignatureTest();
            Version6GenerateAndVerifyInlineSignatureTest();
        }
    }
}