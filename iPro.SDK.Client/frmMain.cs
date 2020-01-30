﻿using EBA.Ex;
using DotNetOpenAuth.OAuth2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iPro.SDK.Client
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();

            CustomStringWriter.Instance.OnWrited = line =>
            {
                try
                {
                    txtHttpRequest.AppendText(line + Environment.NewLine);
                }
                catch (Exception)
                {
                    // ignored
                }
            };

            var now = DateTime.Now;
            txtCreatedate.Text = now.ToUniversalTime().ToString("o");

            txtStartDate.Text = now.AddMonths(1).ToString("yyyy-MM-dd");
            txtEndDate.Text = now.AddMonths(1).AddDays(7).ToString("yyyy-MM-dd");

            txtBookingProperty1Checkin.Text = now.AddMonths(1).ToString("yyyy-MM-dd");
            txtBookingProperty1Checkout.Text = now.AddMonths(1).AddDays(7).ToString("yyyy-MM-dd");

            ddlContactTitle.SelectedText = "Mr";

            txtPropertyRatesApi.Text = txtPropertyRatesApi.Text.Replace("{date}", now.Date.ToString("yyyy-MM-dd"));
        }

        private void HandleWebException(WebException ex)
        {
            var sb = new StringBuilder();

            using (var response = ex.Response)
            {
                try
                {
                    var httpResponse = (HttpWebResponse)response;

                    sb.AppendLine("-------------Message-------------");
                    sb.AppendLine(string.Format("StatusCode:{0}", httpResponse.StatusCode));
                    sb.AppendLine(string.Format("Status:{0}", httpResponse.StatusDescription));

                    using (var data = response.GetResponseStream())
                    {
                        var text = new StreamReader(data).ReadToEnd();
                        sb.AppendLine(text);
                    }
                }
                catch
                {
                }
            }

            sb.AppendLine(string.Empty);
            sb.AppendLine("-------------Error-------------");
            sb.AppendLine(ex.ToString());
            sb.AppendLine(string.Empty);

            outputTextBox.Text = sb.ToString();
        }

        private async Task HandleRequestState(Func<Task> func)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            lblTimeCost.Text = @"Waiting for server response...";

            try
            {
                await func();
            }
            catch (WebException ex)
            {
                HandleWebException(ex);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is WebException)
                {
                    var webException = (WebException)ex.InnerException;
                    HandleWebException(webException);
                }
                else
                {
                    if (ex.InnerException != null)
                    {
                        outputTextBox.Text = ex.InnerException.ToString();
                    }
                    else
                    {
                        outputTextBox.Text = ex.ToString();
                    }
                }
            }

            stopwatch.Stop();
            lblTimeCost.Text = string.Format("Time cost: {0} ms", stopwatch.ElapsedMilliseconds);
        }

        private async void HandleRequestState(Action action)
        {
            await HandleRequestState(async () =>
            {
                action();
            });
        }

        private WebServerClient CreateOAuth2Client()
        {
            var serverDescription = new AuthorizationServerDescription
            {
                TokenEndpoint = new Uri(txtHost.Text + tokenEndpointTextBox.Text)
            };

            var client = new WebServerClient(serverDescription, oauth2ClientIdTextBox.Text, oauth2ClientSecretTextBox.Text);

            return (client);
        }

        private async Task ParseResponse(HttpWebRequest httpRequest)
        {
            var response = await httpRequest.GetResponseAsync();
            outputTextBox.Text = string.Format("Status Code: {0}\r\n", (int)((HttpWebResponse)response).StatusCode);

            var contentDispositionHeader = response.Headers["Content-Disposition"];
            if (!string.IsNullOrWhiteSpace(contentDispositionHeader))
            {
                outputTextBox.Text += contentDispositionHeader;

                var contentDisposition = new ContentDisposition(contentDispositionHeader);
                var extName = response.ContentType == MediaTypeNames.Application.Pdf ? ".pdf" : Path.GetExtension(contentDisposition.FileName);

                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "All files (*.*)|*.*";
                    dialog.FilterIndex = 1;
                    dialog.RestoreDirectory = true;
                    dialog.DefaultExt = extName;
                    dialog.FileName = contentDisposition.FileName;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        using (var stream = dialog.OpenFile())
                        {
                            var res = response.GetResponseStream();
                            res.CopyTo(stream);
                        }
                    }
                }
            }
            else
            {
                var reader = new StreamReader(response.GetResponseStream());
                outputTextBox.Text += reader.ReadToEnd();
            }
        }

        private void GetAccessToken()
        {
            HandleRequestState(() =>
            {
                var client = CreateOAuth2Client();
                var authState = client.GetClientAccessToken();
                accessTokenTextBox.Text = authState.AccessToken;
                tokenExpiryTextBox.Text = authState.AccessTokenExpirationUtc.Value.ToString("O");
                getResourceButton.Enabled = true;
            });
        }

        private async Task LoadContent(string api)
        {
            await HandleRequestState(async () =>
            {
                var url = txtHost.Text.TrimEnd('/') + '/' + api.TrimStart('/');
                var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                ClientBase.AuthorizeRequest(httpRequest, accessTokenTextBox.Text);

                httpRequest.Headers.Add("version", "2.0");
                if (!string.IsNullOrEmpty(txtIfModifiedSince.Text))
                {
                    httpRequest.IfModifiedSince = Convert.ToDateTime(txtIfModifiedSince.Text);
                }

                await ParseResponse(httpRequest);
            });
        }

        private async Task PostContent(string api, byte[] buffer, string contentType = "application/x-www-form-urlencoded")
        {
            await HandleRequestState(async () =>
            {
                var httpRequest = (HttpWebRequest)WebRequest.Create(txtHost.Text + api);
                httpRequest.Method = "POST";
                httpRequest.Headers.Add("version", "2.0");

                httpRequest.ContentLength = buffer.Length;
                httpRequest.ContentType = contentType;
                ClientBase.AuthorizeRequest(httpRequest, accessTokenTextBox.Text);

                using (var postStream = httpRequest.GetRequestStream())
                {
                    postStream.Write(buffer, 0, buffer.Length);
                    postStream.Flush();
                    postStream.Close();
                }

                await ParseResponse(httpRequest);
            });
        }

        /******************************************************************************************************************************************************/

        protected void exchangeCredentialsButton_Click(object sender, EventArgs e)
        {
            GetAccessToken();
        }

        private async void getResourceButton_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyApi.Text);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertImagesApi.Text);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyEnquiresApi.Text);
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyRatesApi.Text);
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyAvailabilityApi.Text);
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertiesApi.Text);
        }

        private async void btnPropertyAll_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyAllApi.Text);
        }

        private async void btnBookingRules_Click(object sender, EventArgs e)
        {
            await LoadContent(txtBookingRules.Text);
        }

        private async void btnBookingTags_Click(object sender, EventArgs e)
        {
            await LoadContent(txtBookingTags.Text);
        }

        private async void btnPropertyExtras_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyExtras.Text);
        }

        private async void btnGetSources_Click(object sender, EventArgs e)
        {
            await LoadContent(txtSources.Text);
        }

        private async void btnContacts_Click(object sender, EventArgs e)
        {
            await LoadContent(txtContacts.Text);
        }

        private async void btnPropertyRooms_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyRoomsApi.Text);
        }

        private async void btnPropertyDistance_Click(object sender, EventArgs e)
        {
            await LoadContent(txtPropertyDistanceApi.Text);
        }

        private async void btnPostBooking_Click(object sender, EventArgs e)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("BrandId", txtBookingBrandId.Text),

                new KeyValuePair<string, string>("BookingTagIds", txtBookingTagIds.Text),
                new KeyValuePair<string, string>("EnquiryId", txtBookingEnquiryId.Text),
                new KeyValuePair<string, string>("Source", txtBookingSource.Text),
                new KeyValuePair<string, string>("SendEmail", cbBookingSendEmail.Checked.ToString()),

                new KeyValuePair<string, string>("PaidAmount", txtPaidAmount.Text),
                new KeyValuePair<string, string>("PaymentMethod", txtBookingPaymentMethod.Text),
                new KeyValuePair<string, string>("PaymentToken", txtBookingPaymentToken.Text),
                new KeyValuePair<string, string>("CardPartialNumbers", txtBookingCardPartialNumbers.Text),
                new KeyValuePair<string, string>("CardType", txtCreateBookingCardType.Text),

                new KeyValuePair<string, string>("IsDeferredPayment", chkBookingIsDeferredPayment.Checked.ToString()),
                new KeyValuePair<string, string>("SagepaySecurityKey", txtBookingSagepaySecurityKey.Text),
                new KeyValuePair<string, string>("SagepayVendorTxCode", txtBookingSagepayVendorTxCode.Text),
                new KeyValuePair<string, string>("SagepayVPSTxId", txtBookingSagepayVPSTxId.Text),
                new KeyValuePair<string, string>("SagepayTxAuthNo", txtBookingSagepayTxAuthNo.Text),
                new KeyValuePair<string, string>("ReturnUrl", txtReturnUrl.Text),

                new KeyValuePair<string, string>("Contact.ContactId", txtBookingContactId.Text),
                new KeyValuePair<string, string>("Contact.Title", txtBookingContactTitle.Text),
                new KeyValuePair<string, string>("Contact.FirstName", txtBookingContactFirstName.Text),
                new KeyValuePair<string, string>("Contact.LastName", txtBookingContactLastName.Text),
                new KeyValuePair<string, string>("Contact.Email", txtBookingContactEmail.Text),
                new KeyValuePair<string, string>("Contact.Email1", txtBookingContactEmail1.Text),
                new KeyValuePair<string, string>("Contact.Telephone", txtBookingContactTelephone.Text),
                new KeyValuePair<string, string>("Contact.Mobile", txtBookingContactMobile.Text),
                new KeyValuePair<string, string>("Contact.Address1", txtBookingContactAddress1.Text),
                new KeyValuePair<string, string>("Contact.Address2", txtBookingContactAddress2.Text),
                new KeyValuePair<string, string>("Contact.City", txtBookingContactCity.Text),
                new KeyValuePair<string, string>("Contact.County", txtBookingContactCounty.Text),
                new KeyValuePair<string, string>("Contact.Postcode", txtBookingContactPostcode.Text),
                new KeyValuePair<string, string>("Contact.Country", txtBookingContactCountry.Text),
                new KeyValuePair<string, string>("Contact.Source", txtBookingContactSource.Text),

                new KeyValuePair<string, string>("InternalNotes", InternalNotes.Text),
            };

            values.AddRange(GetBookingProperties());
            var formContent = new FormUrlEncodedContent(values);
            await PostContent(txtApiBooking.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private IEnumerable<KeyValuePair<string, string>> GetBookingProperties()
        {
            var InsuranceBreakages = string.Empty;
            if (rdoImportBookingInsuranceBreakages_None.Checked) { InsuranceBreakages = "None"; }
            if (rdoImportBookingInsuranceBreakages_Insurance.Checked) { InsuranceBreakages = "Insurance"; }
            if (rdoImportBookingInsuranceBreakages_BreakageDeposit.Checked) { InsuranceBreakages = "BreakageDeposit"; }

            //https://github.com/iprosoftware/azores/issues/1489#issuecomment-364849337
            string commissionType = string.Empty;
            if (rdoImportBookingDiscountType_DiscountNotDeducted.Checked) { commissionType = "1"; }
            if (rdoImportBookingDiscountType_DiscountDeductedFromCommission.Checked) { commissionType = "2"; }
            if (rdoImportBookingDiscountType_DiscountDeductedFromOwnerAmount.Checked) { commissionType = "3"; }
            if (rdoImportBookingDiscountType_DiscountSpiltBetweenOwnerAndCommission5050.Checked) { commissionType = "4"; }
            if (rdoImportBookingDiscountType_CommissionBasedOnNetPrice.Checked) { commissionType = "5"; }

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Properties[0].Id", txtBookingProperty1Id.Text),
                new KeyValuePair<string, string>("Properties[0].Checkin", txtBookingProperty1Checkin.Text),
                new KeyValuePair<string, string>("Properties[0].Checkout", txtBookingProperty1Checkout.Text),

                new KeyValuePair<string, string>("Properties[0].LeadGuestName", txtBookingProperty1LeadGuestName.Text),
                new KeyValuePair<string, string>("Properties[0].LeadGuestAge", txtBookingProperty1LeadGuestAge.Text),
                new KeyValuePair<string, string>("Properties[0].LeadGuestPassport", txtBookingProperty1LeadGuestPassport.Text),

                new KeyValuePair<string, string>("Properties[0].Adults", txtBookingProperty1Adults.Text),
                new KeyValuePair<string, string>("Properties[0].Children", txtBookingProperty1Children.Text),
                new KeyValuePair<string, string>("Properties[0].Infants", txtBookingProperty1Infants.Text),
                new KeyValuePair<string, string>("Properties[0].InsuranceBreakages", InsuranceBreakages),
                new KeyValuePair<string, string>("Properties[0].CommissionType", commissionType),
                new KeyValuePair<string, string>("Properties[0].VoucherCode", txtImportBookingVoucherCode.Text),

                new KeyValuePair<string, string>("Properties[0].Guests[0].Name", txtBookingProperty1Guest1Name.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[0].Age", txtBookingProperty1Guest1Age.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[0].Passport", txtBookingProperty1Guest1Passport.Text),

                new KeyValuePair<string, string>("Properties[0].Guests[1].Name", txtBookingProperty1Guest2Name.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[1].Age", txtBookingProperty1Guest2Age.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[1].Passport", txtBookingProperty1Guest2Passport.Text),

                new KeyValuePair<string, string>("Properties[0].Extras[0].Id", txtBookingProperty1Extra1Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[0].Qty", txtBookingProperty1Extra1Qty.Text),

                new KeyValuePair<string, string>("Properties[0].Extras[1].Id", txtBookingProperty1Extra2Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[1].Qty", txtBookingProperty1Extra2Qty.Text),
                new KeyValuePair<string, string>("Properties[0].GuestNotes", GuestNotes.Text),
                new KeyValuePair<string, string>("Properties[0].AccommodationCost", txtCustomAccommodationCost.Text),
                new KeyValuePair<string, string>("Properties[0].OwnerAmount", txtCustomOwnerAmount.Text),
            };
        }

        private async void btnPostEnquiry_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("firstname", txtFirstName.Text),
                new KeyValuePair<string, string>("lastname", txtLastName.Text),
                new KeyValuePair<string, string>("propertyids", txtPropertyIDs.Text),
                new KeyValuePair<string, string>("startdate", txtStartDate.Text),
                new KeyValuePair<string, string>("enddate", txtEndDate.Text),
                new KeyValuePair<string, string>("days", txtDays.Text),
                new KeyValuePair<string, string>("budget", txtBudget.Text),
                new KeyValuePair<string, string>("mobile", txtMobile.Text),
                new KeyValuePair<string, string>("phone", txtPhone.Text),
                new KeyValuePair<string, string>("email", txtEmail.Text),
                new KeyValuePair<string, string>("adults", txtAdults.Text),
                new KeyValuePair<string, string>("children", txtChildren.Text),
                new KeyValuePair<string, string>("source", txtSource.Text),
                new KeyValuePair<string, string>("comments", txtComments.Text),
                new KeyValuePair<string, string>("createdate", txtCreatedate.Text),
                new KeyValuePair<string, string>("currency", ddlEnquiryCurrency.Text),
                new KeyValuePair<string, string>("BrandId", txtEnquiryBrandId.Text),

            });

            await PostContent(txtApiImportEnquiry.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private async void btnGetReviews_Click(object sender, EventArgs e)
        {
            await LoadContent(txtReviewsApi.Text);
        }

        private async void btnAddReview_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("ReviewTitle", txtReviewTitle.Text),
                new KeyValuePair<string, string>("rating", txtRating.Text),
                new KeyValuePair<string, string>("ReviewDescription", txtReviewDescription.Text),
                new KeyValuePair<string, string>("ReviewerName", txtReviewerName.Text),
                new KeyValuePair<string, string>("PropertyId",txtPropertyId.Text),
                new KeyValuePair<string, string>("IsApproved",cbIsApproved.Checked.ToString()),
            });

            PostContent(txtReviewsApi.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private async void btnPropertySearch_Click(object sender, EventArgs e)
        {
            LoadContent(txtPropertySearchApi.Text);
        }

        private async void btnLocations_Click(object sender, EventArgs e)
        {
            LoadContent(txtLocations.Text);
        }

        private async void btnDayAvailability_Click(object sender, EventArgs e)
        {
            LoadContent(txtDayAvailability.Text);
        }

        private async void btnAmenities_Click(object sender, EventArgs e)
        {
            LoadContent(txtAmenities.Text);
        }

        private async void btnAddPayment_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Amount", txtAmount.Text),
                new KeyValuePair<string, string>("BookingId", txtBookingId.Text),
                new KeyValuePair<string, string>("Charges", txtCharges.Text),
                new KeyValuePair<string, string>("Currency", txtCurrency.Text),
                new KeyValuePair<string, string>("Comments",txtComments.Text),
                new KeyValuePair<string, string>("PaymentScheduleIds",txtPaymentScheduleIds.Text),
                new KeyValuePair<string, string>("PaymentMethod",txtPaymentMethod.Text),
                new KeyValuePair<string, string>("PaymentDate",txtPaymentDate.Text),
                new KeyValuePair<string, string>("Status",txtStatus.Text),
                new KeyValuePair<string, string>("PropertyId",txtPaymentPropertyId.Text),
                new KeyValuePair<string, string>("BalanceDueDate",txtBalanceDueDate.Text),
            });

            PostContent(txtPaymentApi.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private async void btnUpdatePropertyApi_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("liveWebsiteURL", txtLiveWebsiteURL.Text)
            });

            PostContent(txtUpdatePropertyApiUrl.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private async void btnCalcBooking_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(GetBookingCalcProperties());
            await PostContent(txtApiBookingCalc.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private IEnumerable<KeyValuePair<string, string>> GetBookingCalcProperties()
        {
            var InsuranceBreakages = string.Empty;
            if (rdoBookingCalcInsuranceBreakages_None.Checked) { InsuranceBreakages = "None"; }
            if (rdoBookingCalcInsuranceBreakages_Insurance.Checked) { InsuranceBreakages = "Insurance"; }
            if (rdoBookingCalcInsuranceBreakages_BreakageDeposit.Checked) { InsuranceBreakages = "BreakageDeposit"; }

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Properties[0].Id", txtPropertyCalcPropertyId.Text),
                new KeyValuePair<string, string>("Properties[0].Checkin", txtPropertyCalcCheckIn.Text),
                new KeyValuePair<string, string>("Properties[0].Checkout", txtPropertyCalcCheckOut.Text),
                new KeyValuePair<string, string>("Properties[0].Adults", txtPropertyCalcAdults.Text),
                new KeyValuePair<string, string>("Properties[0].Children", txtPropertyCalcChildren.Text),
                new KeyValuePair<string, string>("Properties[0].InsuranceBreakages", InsuranceBreakages),
                new KeyValuePair<string, string>("Properties[0].Infants", txtPropertyCalcInfants.Text),
                new KeyValuePair<string, string>("Properties[0].VoucherCode", txtVoucherCode.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[0].Id", txtPropertyExtra1Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[0].Qty", txtPropertyExtra1Qty.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[1].Id", txtPropertyExtra2Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[1].Qty", txtPropertyExtra2Qty.Text),
            };
        }

        private void btnContactPost_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Title", ddlContactTitle.SelectedItem.ToString()),
                new KeyValuePair<string, string>("FirstName", txtContactFirstName.Text),
                new KeyValuePair<string, string>("LastName", txtContactLastName.Text),
                new KeyValuePair<string, string>("Email", txtContactEmail.Text),
                new KeyValuePair<string, string>("EmailAlt",txtContactEmailAlt.Text),
                new KeyValuePair<string, string>("EmailAlt1",txtContactEmailAlt1.Text),
                new KeyValuePair<string, string>("Telephone",txtContactTelephone.Text),
                new KeyValuePair<string, string>("TelephoneAlt",txtContactTelephoneAlt.Text),
                new KeyValuePair<string, string>("Mobile",txtContactMobile.Text),
                new KeyValuePair<string, string>("Postcode",txtContactPostcode.Text),
                new KeyValuePair<string, string>("Address",txtContactAddress.Text),
                new KeyValuePair<string, string>("StreetName",txtContactStreetName.Text),
                new KeyValuePair<string, string>("TownCity",txtContactCity.Text),
                new KeyValuePair<string, string>("CountyArea",txtContactCountyArea.Text),
                new KeyValuePair<string, string>("CountryCode",txtContactCountryCode.Text),
                new KeyValuePair<string, string>("CompanyName",txtContactCompanyName.Text),
                new KeyValuePair<string, string>("Comments",txtContactComments.Text),
                new KeyValuePair<string, string>("TypeId",txtContactTypeId.Text),
                new KeyValuePair<string, string>("ContactByPost",cbContactByPost.Checked.ToString()),
                new KeyValuePair<string, string>("ContactByEmail",cbContactByEmail.Checked.ToString()),
                new KeyValuePair<string, string>("ContactByPhone",cbContactByPhone.Checked.ToString()),
                new KeyValuePair<string, string>("ContactBySms",cbContactBySms.Checked.ToString()),
                new KeyValuePair<string, string>("SubscribedToMailingList",cbContactOnEmailList.Checked.ToString()),
                new KeyValuePair<string, string>("Commision",txtContactCommision.Text),
                new KeyValuePair<string, string>("Balance",txtContactBalance.Text),
                new KeyValuePair<string, string>("Retainer",txtContactRetainer.Text),
                new KeyValuePair<string, string>("BrandId",txtContactBrandId.Text),

            });

            PostContent(txtContactPostUrl.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private static IEnumerable<string> SplitComma(string text)
        {
            return text
                .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
        }

        private void btnPushProperty_PreviewPayload_Click(object sender, EventArgs e)
        {
            var attributes = new Dictionary<string, List<string>>();
            attributes.Add(pushProperty_Attribute1Name.Text, new List<string>(SplitComma(pushProperty_Attribute1Values.Text)));
            attributes.Add(pushProperty_Attribute2Name.Text, new List<string>(SplitComma(pushProperty_Attribute2Values.Text)));
            attributes.Add(pushProperty_Attribute3Name.Text, new List<string>(SplitComma(pushProperty_Attribute3Values.Text)));
            attributes.Add(pushProperty_Attribute4Name.Text, new List<string>(SplitComma(pushProperty_Attribute4Values.Text)));

            var distances = new List<object>();
            distances.Add(new
            {
                Name = pushProperty_Distance1Name.Text,
                Distance = pushProperty_Distance1Distance.Text,
                DistanceType = pushProperty_Distance1DistanceType.Text,
                DistanceUnit = pushProperty_Distance1DistanceUnit.Text,
            });
            distances.Add(new
            {
                Name = pushProperty_Distance2Name.Text,
                Distance = pushProperty_Distance2Distance.Text,
                DistanceType = pushProperty_Distance2DistanceType.Text,
                DistanceUnit = pushProperty_Distance2DistanceUnit.Text,
            });

            var rooms = new List<object>();
            rooms.Add(new
            {
                // Id = null,
                NodeName = pushProperty_Room1NodeName.Text,
                Name = pushProperty_Room1Name.Text,
                Description = pushProperty_Room1Description.Text,
                Amenities = SplitComma(pushProperty_Room1Amenities.Text).ToList(),
                RoomType = "BATHROOM",
                Type = 1, // 0:Bedroom 1:OtherRoom
            });
            rooms.Add(new
            {
                // Id = null,
                NodeName = pushProperty_Room2NodeName.Text,
                Name = pushProperty_Room2Name.Text,
                Description = pushProperty_Room2Description.Text,
                Amenities = SplitComma(pushProperty_Room2Amenities.Text).ToList(),
                Sleeps = pushProperty_Room2Sleeps.Text,
                RoomNumbers = new List<object>
                {
                    new
                    {
                        Number = "Room Number",
                        Notes = "The Notes",
                    },
                },
                Type = 0, // 0:Bedroom 1:OtherRoom
            });

            var images = new List<object>();
            images.Add(new
            {
                Id = pushProperty_Image1Id.Text,
                Name = pushProperty_Image1Name.Text,
                Source = pushProperty_Image1Source.Text,
            });
            images.Add(new
            {
                Id = pushProperty_Image2Id.Text,
                Name = pushProperty_Image2Name.Text,
                Source = pushProperty_Image2Source.Text,
            });

            var assignedContacts = new List<object>();
            assignedContacts.Add(new
            {
                ContactId = new int?(),
                ExternalId = "abc123",
                ContactType = "KeyHolder",
                Title = "Mr",
                FirstName = "Steve",
                LastName = "Long",
                Email = "steve.long@test.com",
                Email1 = string.Empty,
                Email2 = string.Empty,
                Mobile = string.Empty,
                AltPhone = string.Empty,
                Telephone = string.Empty,
                Address = string.Empty,
                Address1 = string.Empty,
                Postcode = string.Empty,
                County = string.Empty,
                City = string.Empty,
                Country = string.Empty,
                CompanyName = string.Empty,
                Signature = string.Empty,
                Source = string.Empty,
                Comments = string.Empty,
                ByPost = false,
                ByEmail = false,
                ByTelephone = false,
                BySms = false,
                AddedToBlacklist = false,
                OnEmailList = false,
                IsArchived = false,
                EnableAlerts = false,
                AlertEmailTemplateId = new int?(),
            });

            var obj = new
            {
                Id = pushProperty_Id.Text.ConvertToNullable<int>(),
                OwnerCompanyId = pushProperty_OwnerCompanyId.Text.ConvertToNullable<int>(),
                Name = pushProperty_Name.Text,
                PropertyName = pushProperty_PropertyName.Text,
                Title = pushProperty_Title.Text,
                Suspended = pushProperty_Suspended.Checked,
                Withdrawn = pushProperty_Withdrawn.Checked,
                HideOnWebsite = pushProperty_HideOnWebsite.Checked,
                DisableOnlineBooking = pushProperty_DisableOnlineBooking.Checked,
                PropertyReference = pushProperty_PropertyReference.Text,
                ContractRenewalDate = pushProperty_ContractRenewalDate.Text,
                PropertyWebsite = pushProperty_PropertyWebsite.Text,
                Intro = pushProperty_Intro.Text,
                MainDescription = pushProperty_MainDescription.Text,

                BrandId = pushProperty_BrandId.Text.ConvertToNullable<int>(),
                Currency = pushProperty_Currency.Text,
                HideRates = pushProperty_HideRates.Checked,
                MinRate = pushProperty_MinRate.Text.ConvertToNullable<decimal>(),
                MaxRate = pushProperty_MaxRate.Text.ConvertToNullable<decimal>(),
                Commission = pushProperty_Commission.Text.ConvertToNullable<decimal>(),
                BreakagesDeposit = pushProperty_BreakagesDeposit.Text.ConvertToNullable<decimal>(),

                InternalRentalNotes = pushProperty_InternalRentalNotes.Text,
                AvailabilityNotes = pushProperty_AvailabilityNotes.Text,
                RentalNotesTitle = pushProperty_RentalNotesTitle.Text,
                RentalNotes = pushProperty_RentalNotes.Text,
                RentalNotesTitle1 = pushProperty_RentalNotesTitle1.Text,
                RentalNotes1 = pushProperty_RentalNotes1.Text,
                VirtualTourTitle = pushProperty_VirtualTourTitle.Text,
                VirtualTour = pushProperty_VirtualTour.Text,
                Directions = string.Empty,
                BrochurePage = new int?(),

                CheckInTimeFrom = "14:00",
                CheckInTimeTo = "18:00",
                CheckOutTimeUntil = "12:00",

                KeySafeCode = string.Empty,
                WifiCode = string.Empty,
                OwnersCode = string.Empty,
                OfficeCode = string.Empty,

                PropertyNameTitle = string.Empty,
                PropertySummary = string.Empty,
                PropertyDescription = string.Empty,
                RegionDescription = string.Empty,
                LocationDescription = string.Empty,
                OwnerListingStory = string.Empty,

                Address = pushProperty_Address.Text,
                Address2 = pushProperty_Address2.Text,
                City = pushProperty_City.Text,
                County = pushProperty_County.Text,
                Postcode = pushProperty_Postcode.Text,
                Country = pushProperty_Country.Text,
                OwnerPropertyName = string.Empty,

                Url = pushProperty_Url.Text,
                GeoLocation = pushProperty_GeoLocation.Text,
                Pros = pushProperty_Pros.Text,
                Cons = pushProperty_Cons.Text,
                BuildSize = pushProperty_BuildSize.Text,
                PlotSize = pushProperty_PlotSize.Text,
                Licence = pushProperty_Licence.Text,
                LicenceExpiryDate = pushProperty_LicenceExpiryDate.Text,
                Warnings = pushProperty_Warning.Text,
                Location = SplitComma(pushProperty_Location.Text).ToList(),
                TrustPilotTag = pushProperty_TrustPilotTag.Text,

                SEOTitle = pushProperty_SEOTitle.Text,
                SEOKeywords = pushProperty_SEOKeywords.Text,
                SEODescription = pushProperty_SEODescription.Text,

                Attributes = attributes,
                Rooms = rooms,
                Distances = distances,
                Images = images,
                AssignedContacts = assignedContacts,
            };

            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            pushProperty_Payload.Text = json;
        }

        private void btnPushProperty_Post_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(pushProperty_Payload.Text))
            {
                MessageBox.Show("Please preview payload first");
                return;
            }

            var content = new StringContent(pushProperty_Payload.Text);
            PostContent(txtPushPropertyUrl.Text, content.ReadAsByteArrayAsync().Result, "application/json");
        }

        private void btnContactTypes_Click(object sender, EventArgs e)
        {
            LoadContent(txtContactTypesUrl.Text);
        }

        private void btnReferenceLookup_Click(object sender, EventArgs e)
        {
            LoadContent(txtPropertyReferenceLookupApiUrl.Text);
        }

        private void btnCustomRates_Click(object sender, EventArgs e)
        {
            LoadContent(txtCustomRatesApiUrl.Text);
        }

        private void btnGetReservations_Click(object sender, EventArgs e)
        {
            LoadContent(txtReservationsApiUrl.Text);
        }

        private void btnLateDeals_Click(object sender, EventArgs e)
        {
            LoadContent(txtLateDealsApiUrl.Text);
        }

        private void btnGetSpecialOffers_Click(object sender, EventArgs e)
        {
            LoadContent(txtSpecialOffersApiUrl.Text);
        }

        private void btnExternalContact_Click(object sender, EventArgs e)
        {
            LoadContent(txtExternalContactIdUrl.Text);
        }

        private void btnGetContact_Click(object sender, EventArgs e)
        {
            LoadContent(txtGetContactUrl.Text);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            LoadContent(txtSearchApi.Text);
        }

        private void btnGetVouchers_Click(object sender, EventArgs e)
        {
            LoadContent(txtVouchersApiUrl.Text);

        }

        private void button6_Click(object sender, EventArgs e)
        {
            LoadContent(txtStatementsApiUrl.Text);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            LoadContent(txtWelcomePackApiUrl.Text);
        }

        private void btnGetPropertyExteneded_Click(object sender, EventArgs e)
        {
            LoadContent(txtPropertyExtendedApiUrl.Text);
        }

        private void btnPropertySearchLite_Click(object sender, EventArgs e)
        {
            LoadContent(txtPropertySearchLiteApi.Text);
        }


        private void btnCancelBooking_Click(object sender, EventArgs e)
        {
            LoadContent(txtCancelBooking.Text);
        }

        private void btnReactivateBooking_Click(object sender, EventArgs e)
        {
            LoadContent(txtReactivateBooking.Text);
        }

        private void btnAvailabilityCheck_Click(object sender, EventArgs e)
        {
            LoadContent(txtPropertyDayAvailabilityCheckApi.Text);
        }

        private void btnPropertiesLastUpdated_Click(object sender, EventArgs e)
        {
            LoadContent(txtPropertiesLastUpdated.Text);
        }

        private void btnBookingUpdate_Click(object sender, EventArgs e)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("GuestNotes", textGuestNotes.Text),
                new KeyValuePair<string, string>("HouseKeeperNotes", txtHousekeeperNotes.Text),
                new KeyValuePair<string, string>("InternalNotes", txtInternalNotes.Text),
            };

            var formContent = new FormUrlEncodedContent(values);
            PostContent(txtBookingUpdateApiUrl.Text, formContent.ReadAsByteArrayAsync().Result);
        }
    }
}
