﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotNetOpenAuth.OAuth2;

namespace iPro.SDK.Client
{
    public partial class frmMain : Form
    {
        IAuthorizationState authState;

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
        }

        public void UpdateFields()
        {
            accessTokenTextBox.Text = authState.AccessToken;
            tokenExpiryTextBox.Text = authState.AccessTokenExpirationUtc.Value.ToString("O");
            getResourceButton.Enabled = true;
        }

        protected void exchangeCredentialsButton_Click(object sender, EventArgs e)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var client = CreateOAuth2Client();
                authState = client.GetClientAccessToken();
                UpdateFields();

                stopwatch.Stop();

                lblTimeCost.Text = string.Format("Time cost: {0} ms", stopwatch.ElapsedMilliseconds);
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

        }

        private void HandleWebException(WebException ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-------------Error-------------");

            sb.AppendLine(ex.ToString());

            sb.AppendLine("");


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
            sb.AppendLine("");

            outputTextBox.Text = sb.ToString();
        }

        private WebServerClient CreateOAuth2Client()
        {
            var serverDescription = new AuthorizationServerDescription
            {
                TokenEndpoint = new Uri(this.txtHost.Text + tokenEndpointTextBox.Text)
            };
            var client = new WebServerClient(serverDescription, oauth2ClientIdTextBox.Text, oauth2ClientSecretTextBox.Text);
            return (client);
        }

        private async Task LoadContent(string api)
        {
            try
            {
                lblTimeCost.Text = @"Waiting for server response...";
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var url = txtHost.Text.TrimEnd('/') + '/' + api.TrimStart('/');
                var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                ClientBase.AuthorizeRequest(httpRequest, accessTokenTextBox.Text);

                httpRequest.Headers.Add("version", "2.0");
                if (!string.IsNullOrEmpty(txtIfModifiedSince.Text))
                {
                    httpRequest.IfModifiedSince = Convert.ToDateTime(txtIfModifiedSince.Text);
                }

                var response = await httpRequest.GetResponseAsync();

                var reader = new StreamReader(response.GetResponseStream());

                outputTextBox.Text = string.Format("Status Code: {0}\r\n", (int)((HttpWebResponse)response).StatusCode);
                outputTextBox.Text += reader.ReadToEnd();

                stopwatch.Stop();

                lblTimeCost.Text = string.Format("Time cost: {0} ms", stopwatch.ElapsedMilliseconds);
            }
            catch (WebException ex)
            {
                HandleWebException(ex);
            }
            catch (Exception ex)
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

        private async void getResourceButton_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyApi.Text);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertImagesApi.Text);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyEnquiresApi.Text);
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyRatesApi.Text);
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyAvailabilityApi.Text);
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertiesApi.Text);
        }

        private async void btnPropertyAll_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyAllApi.Text);
        }

        private async void btnBookingRules_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtBookingRules.Text);
        }

        private async void btnBookingTags_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtBookingTags.Text);
        }

        private async void btnPropertyExtras_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyExtras.Text);
        }

        private async void btnGetSources_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtSources.Text);
        }

        private async void btnContacts_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtContacts.Text);
        }

        private async void btnPropertyRooms_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyRoomsApi.Text);
        }

        private async void btnPropertyDistance_Click(object sender, EventArgs e)
        {
            await LoadContent(this.txtPropertyDistanceApi.Text);
        }

        private async void btnPostBooking_Click(object sender, EventArgs e)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("BookingTagIds", this.txtBookingTagIds.Text),
                new KeyValuePair<string, string>("EnquiryId", this.txtBookingEnquiryId.Text),
                new KeyValuePair<string, string>("Source", this.txtBookingSource.Text),
                new KeyValuePair<string, string>("SendEmail", this.cbBookingSendEmail.Checked.ToString()),

                new KeyValuePair<string, string>("PaidAmount", this.txtPaidAmount.Text),
                new KeyValuePair<string, string>("PaymentMethod", this.txtBookingPaymentMethod.Text),
                new KeyValuePair<string, string>("PaymentToken", this.txtBookingPaymentToken.Text),
                new KeyValuePair<string, string>("CardPartialNumbers", this.txtBookingCardPartialNumbers.Text),
                new KeyValuePair<string, string>("CardType", this.txtCreateBookingCardType.Text),

                new KeyValuePair<string, string>("IsDeferredPayment", this.chkBookingIsDeferredPayment.Checked.ToString()),
                new KeyValuePair<string, string>("SagepaySecurityKey", this.txtBookingSagepaySecurityKey.Text),
                new KeyValuePair<string, string>("SagepayVendorTxCode", this.txtBookingSagepayVendorTxCode.Text),
                new KeyValuePair<string, string>("SagepayVPSTxId", this.txtBookingSagepayVPSTxId.Text),
                new KeyValuePair<string, string>("SagepayTxAuthNo", this.txtBookingSagepayTxAuthNo.Text),

                new KeyValuePair<string, string>("Contact.ContactId", this.txtBookingContactId.Text),
                new KeyValuePair<string, string>("Contact.Title", this.txtBookingContactTitle.Text),
                new KeyValuePair<string, string>("Contact.FirstName", this.txtBookingContactFirstName.Text),
                new KeyValuePair<string, string>("Contact.LastName", this.txtBookingContactLastName.Text),
                new KeyValuePair<string, string>("Contact.Email", this.txtBookingContactEmail.Text),
                new KeyValuePair<string, string>("Contact.Email1", this.txtBookingContactEmail1.Text),
                new KeyValuePair<string, string>("Contact.Telephone", this.txtBookingContactTelephone.Text),
                new KeyValuePair<string, string>("Contact.Mobile", this.txtBookingContactMobile.Text),
                new KeyValuePair<string, string>("Contact.Address1", this.txtBookingContactAddress1.Text),
                new KeyValuePair<string, string>("Contact.Address2", this.txtBookingContactAddress2.Text),
                new KeyValuePair<string, string>("Contact.City", this.txtBookingContactCity.Text),
                new KeyValuePair<string, string>("Contact.County", this.txtBookingContactCounty.Text),
                new KeyValuePair<string, string>("Contact.Postcode", this.txtBookingContactPostcode.Text),
                new KeyValuePair<string, string>("Contact.Country", this.txtBookingContactCountry.Text),
                new KeyValuePair<string, string>("Contact.Source", this.txtBookingContactSource.Text)
            };

            values.AddRange(this.GetBookingProperties());
            var formContent = new FormUrlEncodedContent(values);
            await PostContent(this.txtApiBooking.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private IEnumerable<KeyValuePair<string, string>> GetBookingProperties()
        {
            var InsuranceBreakages = string.Empty;
            if (rdoImportBookingInsuranceBreakages_None.Checked) { InsuranceBreakages = "None"; }
            if (rdoImportBookingInsuranceBreakages_Insurance.Checked) { InsuranceBreakages = "Insurance"; }
            if (rdoImportBookingInsuranceBreakages_BreakageDeposit.Checked) { InsuranceBreakages = "BreakageDeposit"; }

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Properties[0].Id", this.txtBookingProperty1Id.Text),
                new KeyValuePair<string, string>("Properties[0].Checkin", this.txtBookingProperty1Checkin.Text),
                new KeyValuePair<string, string>("Properties[0].Checkout", this.txtBookingProperty1Checkout.Text),

                new KeyValuePair<string, string>("Properties[0].LeadGuestName", this.txtBookingProperty1LeadGuestName.Text),
                new KeyValuePair<string, string>("Properties[0].LeadGuestAge", this.txtBookingProperty1LeadGuestAge.Text),
                new KeyValuePair<string, string>("Properties[0].LeadGuestPassport", this.txtBookingProperty1LeadGuestPassport.Text),

                new KeyValuePair<string, string>("Properties[0].Adults", this.txtBookingProperty1Adults.Text),
                new KeyValuePair<string, string>("Properties[0].Children", this.txtBookingProperty1Children.Text),
                new KeyValuePair<string, string>("Properties[0].Infants", this.txtBookingProperty1Infants.Text),
                new KeyValuePair<string, string>("Properties[0].InsuranceBreakages", InsuranceBreakages),

                new KeyValuePair<string, string>("Properties[0].Guests[0].Name", this.txtBookingProperty1Guest1Name.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[0].Age", this.txtBookingProperty1Guest1Age.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[0].Passport", this.txtBookingProperty1Guest1Passport.Text),

                new KeyValuePair<string, string>("Properties[0].Guests[1].Name", this.txtBookingProperty1Guest2Name.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[1].Age", this.txtBookingProperty1Guest2Age.Text),
                new KeyValuePair<string, string>("Properties[0].Guests[1].Passport", this.txtBookingProperty1Guest2Passport.Text),

                new KeyValuePair<string, string>("Properties[0].Extras[0].Id", this.txtBookingProperty1Extra1Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[0].Qty", this.txtBookingProperty1Extra1Qty.Text),


                new KeyValuePair<string, string>("Properties[0].Extras[1].Id", this.txtBookingProperty1Extra2Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[1].Qty", this.txtBookingProperty1Extra2Qty.Text)
            };
        }

        private async void btnPostEnquiry_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("firstname", this.txtFirstName.Text),
                new KeyValuePair<string, string>("lastname", this.txtLastName.Text),
                new KeyValuePair<string, string>("propertyids", this.txtPropertyIDs.Text),
                new KeyValuePair<string, string>("startdate", this.txtStartDate.Text),
                new KeyValuePair<string, string>("enddate", this.txtEndDate.Text),
                new KeyValuePair<string, string>("days", this.txtDays.Text),
                new KeyValuePair<string, string>("budget", this.txtBudget.Text),
                new KeyValuePair<string, string>("mobile", this.txtMobile.Text),
                new KeyValuePair<string, string>("phone", this.txtPhone.Text),
                new KeyValuePair<string, string>("email", this.txtEmail.Text),
                new KeyValuePair<string, string>("adults", this.txtAdults.Text),
                new KeyValuePair<string, string>("children", this.txtChildren.Text),
                new KeyValuePair<string, string>("source", this.txtSource.Text),
                new KeyValuePair<string, string>("comments", this.txtComments.Text),
                new KeyValuePair<string, string>("createdate", this.txtCreatedate.Text)
            });

            await PostContent(this.txtApiImportEnquiry.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private async Task PostContent(string api, byte[] buffer)
        {
            try
            {
                lblTimeCost.Text = @"Waiting for server response...";

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var httpRequest = (HttpWebRequest)WebRequest.Create(this.txtHost.Text + api);
                httpRequest.Method = "POST";
                httpRequest.Headers.Add("version", "2.0");

                httpRequest.ContentLength = buffer.Length;
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                ClientBase.AuthorizeRequest(httpRequest, accessTokenTextBox.Text);

                using (var postStream = httpRequest.GetRequestStream())
                {
                    postStream.Write(buffer, 0, buffer.Length);
                    postStream.Flush();
                    postStream.Close();
                }

                var response = await httpRequest.GetResponseAsync();

                var reader = new StreamReader(response.GetResponseStream());

                outputTextBox.Text = string.Format("Status Code: {0}\r\n", (int)((HttpWebResponse)response).StatusCode);
                outputTextBox.Text += reader.ReadToEnd();

                stopwatch.Stop();

                lblTimeCost.Text = string.Format("Time cost: {0} ms", stopwatch.ElapsedMilliseconds);
            }
            catch (WebException ex)
            {
                HandleWebException(ex);
            }
            catch (Exception ex)
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
                new KeyValuePair<string, string>("IsApproved",cbIsApproved.Checked.ToString())
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
                new KeyValuePair<string, string>("PropertyId",txtPaymentPropertyId.Text)
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
            var formContent = new FormUrlEncodedContent(this.GetBookingCalcProperties());
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
                new KeyValuePair<string, string>("Properties[0].Id", this.txtPropertyCalcPropertyId.Text),
                new KeyValuePair<string, string>("Properties[0].Checkin", this.txtPropertyCalcCheckIn.Text),
                new KeyValuePair<string, string>("Properties[0].Checkout", this.txtPropertyCalcCheckOut.Text),
                new KeyValuePair<string, string>("Properties[0].Adults", this.txtPropertyCalcAdults.Text),
                new KeyValuePair<string, string>("Properties[0].Children", this.txtPropertyCalcChildren.Text),
                new KeyValuePair<string, string>("Properties[0].InsuranceBreakages", InsuranceBreakages),
                new KeyValuePair<string, string>("Properties[0].Infants", this.txtPropertyCalcInfants.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[0].Id", this.txtPropertyExtra1Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[0].Qty", this.txtPropertyExtra1Qty.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[1].Id", this.txtPropertyExtra2Id.Text),
                new KeyValuePair<string, string>("Properties[0].Extras[1].Qty", this.txtPropertyExtra2Qty.Text)
            };
        }

        private void btnContactPost_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Title", ddlContactTitle.SelectedText),
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
                new KeyValuePair<string, string>("DoNotMail",cbContactDoNotMail.Checked.ToString()),
                new KeyValuePair<string, string>("DoNotEmail",cbContactDoNotEmail.Checked.ToString()),
                new KeyValuePair<string, string>("DoNotPhone",cbContactDoNotPhone.Checked.ToString()),
                new KeyValuePair<string, string>("OnEmailList",cbContactOnEmailList.Checked.ToString()),
                new KeyValuePair<string, string>("Commision",txtContactCommision.Text),
                new KeyValuePair<string, string>("Balance",txtContactBalance.Text),
                new KeyValuePair<string, string>("Retainer",txtContactRetainer.Text)
            });

            PostContent(txtContactPostUrl.Text, formContent.ReadAsByteArrayAsync().Result);
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
    }
}
