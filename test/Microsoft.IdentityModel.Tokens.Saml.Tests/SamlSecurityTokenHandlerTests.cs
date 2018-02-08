//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
#if System
using System.IdentityModel.Tokens.Jwt;
#else
using Microsoft.IdentityModel.Tokens.Jwt;
#endif
using System.IO;
using System.Security.Claims;
using System.Xml;
using Microsoft.IdentityModel.Tests;
using Microsoft.IdentityModel.Xml;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Tokens.Saml.Tests
{
    /// <summary>
    /// 
    /// </summary>
    public class SamlSecurityTokenHandlerTests
    {
        [Fact]
        public void Constructors()
        {
            SamlSecurityTokenHandler samlSecurityTokenHandler = new SamlSecurityTokenHandler();
        }

        [Fact]
        public void Defaults()
        {
            SamlSecurityTokenHandler samlSecurityTokenHandler = new SamlSecurityTokenHandler();
            Assert.True(samlSecurityTokenHandler.MaximumTokenSizeInBytes == TokenValidationParameters.DefaultMaximumTokenSizeInBytes, "MaximumTokenSizeInBytes");
        }

        [Fact]
        public void GetSets()
        {
            var samlSecurityTokenHandler = new SamlSecurityTokenHandler();
            var context = new GetSetContext
            {
                PropertyNamesAndSetGetValue = new List<KeyValuePair<string, List<object>>>
                {
                    new KeyValuePair<string, List<object>>("MaximumTokenSizeInBytes", new List<object>{(object)TokenValidationParameters.DefaultMaximumTokenSizeInBytes, (object)1000, (object)10}),
                    new KeyValuePair<string, List<object>>("SetDefaultTimesOnTokenCreation", new List<object>{true, false, true}),
                    new KeyValuePair<string, List<object>>("TokenLifetimeInMinutes", new List<object>{(object)60, (object)1000, (object)10}),
                },
                Object = samlSecurityTokenHandler
            };

            TestUtilities.GetSet(context);

            samlSecurityTokenHandler = new SamlSecurityTokenHandler();
            TestUtilities.SetGet(samlSecurityTokenHandler, "MaximumTokenSizeInBytes", (object)0, ExpectedException.ArgumentOutOfRangeException(substringExpected: "IDX10101"), context);
            TestUtilities.SetGet(samlSecurityTokenHandler, "MaximumTokenSizeInBytes", (object)1, ExpectedException.NoExceptionExpected, context);
            TestUtilities.SetGet(samlSecurityTokenHandler, "Serializer", null, ExpectedException.ArgumentNullException(), context);

            TestUtilities.AssertFailIfErrors("Saml2SecurityTokenHandlerTests_GetSets", context.Errors);

        }

        [Theory, MemberData(nameof(CanReadTokenTheoryData))]
        public void CanReadToken(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.CanReadToken", theoryData);
            try
            {
                if (theoryData.CanRead != theoryData.Handler.CanReadToken(theoryData.Token))
                    Assert.False(true, $"Expected CanRead != CanRead, token: {theoryData.Token}");

                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> CanReadTokenTheoryData
        {
            get
            {
                return new TheoryData<SamlTheoryData>
                {
                    new SamlTheoryData
                    {
                        First = true,
                        CanRead = false,
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = "Null Token",
                        Token = null
                    },

                    new SamlTheoryData
                    {
                        CanRead = false,
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = "DefaultMaximumTokenSizeInBytes + 1",
                        Token = new string('S', TokenValidationParameters.DefaultMaximumTokenSizeInBytes + 2)
                    },

                    new SamlTheoryData
                    {
                        CanRead = true,
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_Valid),
                        Token = ReferenceTokens.SamlToken_Valid
                    }
                };
            }
        }

        [Theory, MemberData(nameof(CreateClaimsIdentitiesTheoryData))]
        public void CreateClaimsIdentities(SamlTheoryData theoryData)
        {
            TestUtilities.WriteHeader($"{this}.CreateClaimsIdentities", theoryData);
            var context = new CompareContext($"{this}.CreateClaimsIdentities, {theoryData.TestId}") { IgnoreType = true };
            try
            {
                var identities = ((theoryData.Handler) as SamlSecurityTokenHandlerPublic).CreateClaimsIdentitiesPublic(theoryData.TokenTestSet.SecurityToken as SamlSecurityToken, theoryData.Issuer, theoryData.ValidationParameters);
                theoryData.ExpectedException.ProcessNoException(context);

                IdentityComparer.AreEqual(identities, theoryData.TokenTestSet.Identities, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> CreateClaimsIdentitiesTheoryData
        {
            get
            {
                return new TheoryData<SamlTheoryData>
                {
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenException), "IDX11313:"),
                        First = true,
                        Handler = new SamlSecurityTokenHandlerPublic(),
                        Issuer = Default.Issuer,
                        TestId = nameof(ReferenceSaml.TokenClaimsIdentitiesSubjectEmptyString),
                        TokenTestSet = ReferenceSaml.TokenClaimsIdentitiesSubjectEmptyString,
                        ValidationParameters = new TokenValidationParameters()
                    },
                    new SamlTheoryData
                    {
                        Handler = new SamlSecurityTokenHandlerPublic(),
                        Issuer = Default.Issuer,
                        TestId = nameof(ReferenceSaml.TokenClaimsIdentitiesSameSubject),
                        TokenTestSet = ReferenceSaml.TokenClaimsIdentitiesSameSubject,
                        ValidationParameters = new TokenValidationParameters()
                    },
                    new SamlTheoryData
                    {
                        Handler = new SamlSecurityTokenHandlerPublic(),
                        Issuer = Default.Issuer,
                        TestId = nameof(ReferenceSaml.TokenClaimsIdentitiesDifferentSubjects),
                        TokenTestSet = ReferenceSaml.TokenClaimsIdentitiesDifferentSubjects,
                        ValidationParameters = new TokenValidationParameters()
                    }
                };
            }
        }

        [Theory, MemberData(nameof(ReadTokenTheoryData))]
        public void ReadToken(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ReadToken", theoryData);
            try
            {
                theoryData.Handler.ReadToken(theoryData.Token);
                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> ReadTokenTheoryData
        {
            get
            {
                return new TheoryData<SamlTheoryData>
                {
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.NoExceptionExpected,
                        First = true,
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_Valid),
                        Token = ReferenceTokens.SamlToken_Valid
                    }
                };
            }
        }

        [Theory, MemberData(nameof(RoundTripTokenTheoryData))]
        public void RoundTripToken(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.RoundTripToken", theoryData);
            try
            {
                var samlToken = theoryData.Handler.CreateToken(theoryData.TokenDescriptor);
                var token = theoryData.Handler.WriteToken(samlToken);
                var principal = theoryData.Handler.ValidateToken(token, theoryData.ValidationParameters, out SecurityToken validatedToken);
                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> RoundTripTokenTheoryData
        {
            get => new TheoryData<SamlTheoryData>
            {
                new SamlTheoryData
                {
                    First = true,
                    TestId = nameof(Default.ClaimsIdentity),
                    TokenDescriptor = new SecurityTokenDescriptor
                    {
                        Expires = DateTime.UtcNow + TimeSpan.FromDays(1),
                        Audience = Default.Audience,
                        SigningCredentials = Default.AsymmetricSigningCredentials,
                        Issuer = Default.Issuer,
                        Subject = Default.SamlClaimsIdentity
                    },
                    ValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = Default.AsymmetricSigningKey,
                        ValidAudience = Default.Audience,
                        ValidIssuer = Default.Issuer,
                    }
                },
                new SamlTheoryData
                {
                    TestId = nameof(Default.ClaimsIdentity) + nameof(KeyingMaterial.RsaSigningCreds_2048),
                    TokenDescriptor = new SecurityTokenDescriptor
                    {
                        Expires = DateTime.UtcNow + TimeSpan.FromDays(1),
                        Audience = Default.Audience,
                        SigningCredentials = KeyingMaterial.RsaSigningCreds_2048,
                        Issuer = Default.Issuer,
                        Subject = Default.SamlClaimsIdentity
                    },
                    ValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = KeyingMaterial.RsaSigningCreds_2048_Public.Key,
                        ValidAudience = Default.Audience,
                        ValidIssuer = Default.Issuer,
                    },
                },
                new SamlTheoryData
                {
                    TestId = nameof(Default.ClaimsIdentity) + nameof(KeyingMaterial.RsaSigningCreds_2048_FromRsa),
                    TokenDescriptor = new SecurityTokenDescriptor
                    {
                        Expires = DateTime.UtcNow + TimeSpan.FromDays(1),
                        Audience = Default.Audience,
                        SigningCredentials = KeyingMaterial.RsaSigningCreds_2048_FromRsa,
                        Issuer = Default.Issuer,
                        Subject = Default.SamlClaimsIdentity
                    },
                    ValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = KeyingMaterial.RsaSigningCreds_2048_FromRsa_Public.Key,
                        ValidAudience = Default.Audience,
                        ValidIssuer = Default.Issuer,
                    },
                },
                new SamlTheoryData
                {
                    TestId = nameof(Default.ClaimsIdentity) + nameof(KeyingMaterial.JsonWebKeyRsa256SigningCredentials),
                    TokenDescriptor = new SecurityTokenDescriptor
                    {
                        Expires = DateTime.UtcNow + TimeSpan.FromDays(1),
                        Audience = Default.Audience,
                        SigningCredentials = KeyingMaterial.JsonWebKeyRsa256SigningCredentials,
                        Issuer = Default.Issuer,
                        Subject = Default.SamlClaimsIdentity
                    },
                    ValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = KeyingMaterial.JsonWebKeyRsa256PublicSigningCredentials.Key,
                        ValidAudience = Default.Audience,
                        ValidIssuer = Default.Issuer,
                    }
                }
            };
        }

        // Test checks to make sure that default times are correctly added to the token
        // upon token creation.
        [Fact]
        public void SetDefaultTimesOnTokenCreation()
        {
            TestUtilities.WriteHeader($"{this}.SetDefaultTimesOnTokenCreation");
            var context = new CompareContext();

            var tokenHandler = new SamlSecurityTokenHandler();
            var descriptorNoTimeValues = new SecurityTokenDescriptor()
            {
                Issuer = Default.Issuer,
                Audience = Default.Audience,
                SigningCredentials = Default.AsymmetricSigningCredentials
            };

            var token = tokenHandler.CreateToken(descriptorNoTimeValues);
            var samlSecurityToken = token as SamlSecurityToken;

            Assert.NotEqual(DateTime.MinValue, samlSecurityToken.ValidFrom);
            Assert.NotEqual(DateTime.MinValue, samlSecurityToken.ValidTo);
        }

        [Theory, MemberData(nameof(ValidateAudienceTheoryData))]
        public void ValidateAudience(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ValidateAudience", theoryData);
            try
            {
                ((theoryData.Handler) as SamlSecurityTokenHandlerPublic).ValidateAudiencePublic(theoryData.Audiences, null, theoryData.ValidationParameters);
                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> ValidateAudienceTheoryData
        {
            get
            {
                var tokenTheoryData = new List<TokenTheoryData>();
                ValidateTheoryData.AddValidateAudienceTheoryData(tokenTheoryData);

                var theoryData = new TheoryData<SamlTheoryData>();
                foreach (var item in tokenTheoryData)
                    theoryData.Add(new SamlTheoryData(item)
                    {
                        Handler = new SamlSecurityTokenHandlerPublic()
                    });

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ValidateIssuerTheoryData))]
        public void ValidateIssuer(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ValidateIssuer", theoryData);
            try
            {
                ((theoryData.Handler) as SamlSecurityTokenHandlerPublic).ValidateIssuerPublic(theoryData.Issuer, null, theoryData.ValidationParameters);
                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> ValidateIssuerTheoryData
        {
            get
            {
                var tokenTheoryData = new List<TokenTheoryData>();
                ValidateTheoryData.AddValidateIssuerTheoryData(tokenTheoryData);

                var theoryData = new TheoryData<SamlTheoryData>();
                foreach (var item in tokenTheoryData)
                    theoryData.Add(new SamlTheoryData(item)
                    {
                        Handler = new SamlSecurityTokenHandlerPublic()
                    });

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(ValidateTokenTheoryData))]
        public void ValidateToken(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.ValidateToken", theoryData);

            ClaimsPrincipal retVal = null;
            try
            {
                retVal = (theoryData.Handler as SamlSecurityTokenHandler).ValidateToken(theoryData.Token, theoryData.ValidationParameters, out SecurityToken validatedToken);
                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> ValidateTokenTheoryData
        {
            get
            {
                // uncomment to view exception displayed to user
                // ExpectedException.DefaultVerbose = true;

                return new TheoryData<SamlTheoryData>
                {
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("IDX10000:"),
                        First = true,
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = "Null-SecurityToken",
                        Token = null,
                        ValidationParameters = new TokenValidationParameters()
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.ArgumentNullException("IDX10000:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = "NULL-TokenValidationParameters",
                        Token = "s",
                        ValidationParameters = null,
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.ArgumentException("IDX10209:"),
                        Handler = new SamlSecurityTokenHandler { MaximumTokenSizeInBytes = 1 },
                        TestId = "SecurityTokenTooLarge",
                        Token = "ss",
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11115:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_MissingMajorVersion),
                        Token = ReferenceTokens.SamlToken_MissingMajorVersion,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11115:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_MissingMinorVersion),
                        Token = ReferenceTokens.SamlToken_MissingMinorVersion,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11116:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_MajorVersionNotV1),
                        Token = ReferenceTokens.SamlToken_MajorVersionNotV1,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11117:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_MinorVersionNotV1),
                        Token = ReferenceTokens.SamlToken_MinorVersionNotV1,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11115:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_IdMissing),
                        Token = ReferenceTokens.SamlToken_IdMissing,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11121:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_IdFormatError),
                        Token = ReferenceTokens.SamlToken_IdFormatError,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11115:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_IssuerMissing),
                        Token = ReferenceTokens.SamlToken_IssuerMissing,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11115:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_IssueInstantMissing),
                        Token = ReferenceTokens.SamlToken_IssueInstantMissing,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11122:", typeof(FormatException)),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_IssueInstantFormatError),
                        Token = ReferenceTokens.SamlToken_IssueInstantFormatError,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11120:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_AudienceMissing),
                        Token = ReferenceTokens.SamlToken_AudienceMissing,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11130:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_NoStatements),
                        Token = ReferenceTokens.SamlToken_NoStatements,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11112:", typeof(XmlReadException)),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_NoSubject),
                        Token = ReferenceTokens.SamlToken_NoSubject,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = new ExpectedException(typeof(SamlSecurityTokenReadException), "IDX11131:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_NoAttributes),
                        Token = ReferenceTokens.SamlToken_NoAttributes,
                        ValidationParameters = new TokenValidationParameters(),
                    },
                    new SamlTheoryData
                    {
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = $"{nameof(ReferenceTokens.SamlToken_Valid)} IssuerSigningKey set",
                        Token = ReferenceTokens.SamlToken_Valid,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultX509SigningCreds_2048_RsaSha2_Sha2.Key,
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                        }
                    },
                    new SamlTheoryData
                    {
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = $"{nameof(ReferenceTokens.SamlToken_Valid)} IssuerSigningKey Rsa",
                        Token = ReferenceTokens.SamlToken_Valid_WithRsaKeyValue,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultRsaSecurityKey1,
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                        }
                    },
                    new SamlTheoryData
                    {
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = $"{nameof(ReferenceTokens.SamlToken_Valid)} IssuerSigningKey JsonWithCertificate",
                        Token = ReferenceTokens.SamlToken_Valid,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultJsonWebKeyWithCertificate1,
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                        }
                    },
                    new SamlTheoryData
                    {
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = $"{nameof(ReferenceTokens.SamlToken_Valid)} IssuerSigningKey JsonWithParameters",
                        Token = ReferenceTokens.SamlToken_Valid,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultJsonWebKeyWithParameters1,
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                        }
                    },
                    new SamlTheoryData
                    {
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_Valid_Spaces_Added),
                        Token = ReferenceTokens.SamlToken_Valid_Spaces_Added,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultX509SigningCreds_2048_RsaSha2_Sha2.Key,
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = false,
                        }
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.SecurityTokenInvalidSignatureException("IDX10503:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_AttributeTampered),
                        Token = ReferenceTokens.SamlToken_AttributeTampered,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultX509SigningCreds_2048_RsaSha2_Sha2.Key,
                        }
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.SecurityTokenInvalidSignatureException("IDX10503:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_DigestTampered),
                        Token = ReferenceTokens.SamlToken_DigestTampered,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultX509SigningCreds_2048_RsaSha2_Sha2.Key,
                        }
                    },
                    // Removed until we have a way of matching a KeyInfo with a SecurityKey.
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.SecurityTokenSignatureKeyNotFoundException("IDX10501:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_Valid),
                        Token = ReferenceTokens.SamlToken_Valid,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultAADSigningKey,
                        }
                    },
                    new SamlTheoryData
                    {
                        ExpectedException = ExpectedException.SecurityTokenInvalidSignatureException("IDX10503:"),
                        Handler = new SamlSecurityTokenHandler(),
                        TestId = nameof(ReferenceTokens.SamlToken_SignatureTampered),
                        Token = ReferenceTokens.SamlToken_SignatureTampered,
                        ValidationParameters = new TokenValidationParameters
                        {
                            IssuerSigningKey = KeyingMaterial.DefaultX509SigningCreds_2048_RsaSha2_Sha2.Key,
                        }
                    }
                };
            }
        }

        [Theory, MemberData(nameof(WriteTokenTheoryData))]
        public void WriteToken(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteToken", theoryData);
            context.PropertiesToIgnoreWhenComparing = new Dictionary<Type, List<string>>
            {
                { typeof(SamlAssertion), new List<string> { "IssueInstant", "InclusiveNamespacesPrefixList", "Signature", "SigningCredentials" } },
                { typeof(SamlSecurityToken), new List<string> { "SigningKey" } },
            };

            try
            {
                var token = theoryData.Handler.WriteToken(theoryData.SecurityToken);
                theoryData.Handler.ValidateToken(token, theoryData.ValidationParameters, out SecurityToken validatedToken);
                IdentityComparer.AreEqual(validatedToken, theoryData.SecurityToken, context);
                if (!string.IsNullOrEmpty(theoryData.InclusiveNamespacesPrefixList))
                {
                    if (!string.Equals(theoryData.InclusiveNamespacesPrefixList, (theoryData.SecurityToken as SamlSecurityToken).Assertion.InclusiveNamespacesPrefixList))
                        context.Diffs.Add("!string.Equals(theoryData.InclusivePrefixList, (theoryData.SecurityToken as SamlSecurityToken).Assertion.InclusivePrefixList)");

                    if (!string.Equals(theoryData.InclusiveNamespacesPrefixList, (validatedToken as SamlSecurityToken).Assertion.Signature.SignedInfo.References[0].CanonicalizingTransfrom.InclusiveNamespacesPrefixList))
                        context.Diffs.Add("!string.Equals(theoryData.InclusivePrefixList, (validatedToken as SamlSecurityToken).Assertion.Signature.SignedInfo.References[0].CanonicalizingTransfrom.InclusivePrefixList))");
                }

                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> WriteTokenTheoryData
        {
            get
            {
                var key = KeyingMaterial.X509SecurityKeySelfSigned2048_SHA256;
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Audience = Default.Audience,
                    NotBefore = Default.NotBefore,
                    Expires = Default.Expires,
                    Issuer = Default.Issuer,
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest),
                    Subject = new ClaimsIdentity(Default.SamlClaims)
                };

                var validationParameters = new TokenValidationParameters
                {
                    AuthenticationType = "Federation",
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = false,
                    IssuerSigningKey = key
                };

                var tokenHandler = new SamlSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor) as SamlSecurityToken;
                token.Assertion.InclusiveNamespacesPrefixList = "#default saml ds xml";

                // uncomment to view exception displayed to user
                // ExpectedException.DefaultVerbose = true;
                var theoryData = new TheoryData<SamlTheoryData>();

                theoryData.Add(new SamlTheoryData
                {
                    ExpectedException = ExpectedException.ArgumentNullException("token"),
                    First = true,
                    SecurityToken = null,
                    TestId = nameof(ReferenceSaml.NullToken),
                });

                theoryData.Add(new SamlTheoryData
                {
                    ExpectedException = ExpectedException.ArgumentException("IDX11400:"),
                    SecurityToken = new JwtSecurityToken(Default.AsymmetricJwt),
                    TestId = nameof(ReferenceSaml.JwtToken),
                });

                theoryData.Add(new SamlTheoryData
                {
                    InclusiveNamespacesPrefixList = "#default saml ds xml",
                    SecurityToken = token,
                    TestId = "WithInclusivePrefixList",
                    ValidationParameters = validationParameters
                });

                theoryData.Add(new SamlTheoryData
                {
                    SecurityToken = tokenHandler.CreateToken(tokenDescriptor),
                    TestId = "WithoutInclusivePrefixList",
                    ValidationParameters = validationParameters
                });

                validationParameters = new TokenValidationParameters
                {
                    AuthenticationType = "Federation",
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyValidator = ValidationDelegates.IssuerSecurityKeyValidatorThrows,
                    IssuerSigningKey = key
                };

                theoryData.Add(new SamlTheoryData
                {
                    ExpectedException = new ExpectedException(typeof(SecurityTokenInvalidSigningKeyException)),
                    SecurityToken = tokenHandler.CreateToken(tokenDescriptor),
                    TestId = nameof(ValidationDelegates.IssuerSecurityKeyValidatorThrows),
                    ValidationParameters = validationParameters
                });

                validationParameters = new TokenValidationParameters
                {
                    AuthenticationType = "Federation",
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    IssuerSigningKeyValidator = ValidationDelegates.IssuerSecurityKeyValidatorThrows,
                    IssuerSigningKey = key
                };

                theoryData.Add(new SamlTheoryData
                {
                    SecurityToken = tokenHandler.CreateToken(tokenDescriptor),
                    TestId = nameof(ValidationDelegates.IssuerSecurityKeyValidatorThrows) + "-false",
                    ValidationParameters = validationParameters
                });

                validationParameters = new TokenValidationParameters
                {
                    AuthenticationType = "Federation",
                    ValidateAudience = true,
                    ValidateIssuer = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    AudienceValidator = ValidationDelegates.AudienceValidatorThrows,
                    IssuerSigningKey = key
                };

                theoryData.Add(new SamlTheoryData
                {
                    ExpectedException = new ExpectedException(typeof(SecurityTokenInvalidAudienceException)),
                    SecurityToken = tokenHandler.CreateToken(tokenDescriptor),
                    TestId = nameof(ValidationDelegates.AudienceValidatorThrows),
                    ValidationParameters = validationParameters
                });

                validationParameters = new TokenValidationParameters
                {
                    AuthenticationType = "Federation",
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    AudienceValidator = ValidationDelegates.AudienceValidatorThrows,
                    IssuerSigningKey = key
                };

                theoryData.Add(new SamlTheoryData
                {
                    SecurityToken = tokenHandler.CreateToken(tokenDescriptor),
                    TestId = nameof(ValidationDelegates.AudienceValidatorThrows) + "-false",
                    ValidationParameters = validationParameters
                });

                return theoryData;
            }
        }

        [Theory, MemberData(nameof(WriteTokenXmlTheoryData))]
        public void WriteTokenXml(SamlTheoryData theoryData)
        {
            var context = TestUtilities.WriteHeader($"{this}.WriteTokenXml", theoryData);
            try
            {
                (theoryData.Handler as SamlSecurityTokenHandler).WriteToken(theoryData.XmlWriter, theoryData.TokenTestSet.SecurityToken);
                theoryData.ExpectedException.ProcessNoException(context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<SamlTheoryData> WriteTokenXmlTheoryData
        {
            get
            {
                // uncomment to view exception displayed to user
                // ExpectedException.DefaultVerbose = true;

                var theoryData = new TheoryData<SamlTheoryData>();
                var memoryStream = new MemoryStream();
                theoryData.Add(new SamlTheoryData
                {
                    ExpectedException = ExpectedException.ArgumentNullException("token"),
                    First = true,
                    TestId = nameof(ReferenceSaml.NullToken),
                    TokenTestSet = ReferenceSaml.NullToken,
                    XmlWriter = XmlDictionaryWriter.CreateTextWriter(memoryStream)
                });

                theoryData.Add(new SamlTheoryData
                {
                    ExpectedException = ExpectedException.ArgumentNullException("writer"),
                    TestId = "Null XmlWriter",
                    TokenTestSet = ReferenceSaml.SamlSecurityTokenValid
                });

                memoryStream = new MemoryStream();
                theoryData.Add(new SamlTheoryData
                {
                    ExpectedException = ExpectedException.ArgumentException("IDX11400:"),
                    MemoryStream = memoryStream,
                    TestId = nameof(ReferenceSaml.JwtToken),
                    TokenTestSet = ReferenceSaml.JwtToken,
                    XmlWriter = XmlDictionaryWriter.CreateTextWriter(memoryStream)
                });

                return theoryData;
            }
        }

        private class SamlSecurityTokenHandlerPublic : SamlSecurityTokenHandler
        {
            public IEnumerable<ClaimsIdentity> CreateClaimsIdentitiesPublic(SamlSecurityToken samlToken, string issuer, TokenValidationParameters validationParameters)
            {
                return base.CreateClaimsIdentities(samlToken, issuer, validationParameters);
            }

            public void ValidateAudiencePublic(IEnumerable<string> audiences, SecurityToken token, TokenValidationParameters validationParameters)
            {
                base.ValidateAudience(audiences, token, validationParameters);
            }

            public string ValidateIssuerPublic(string issuer, SecurityToken token, TokenValidationParameters validationParameters)
            {
                return base.ValidateIssuer(issuer, token, validationParameters);
            }
        }
    }
}

#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
