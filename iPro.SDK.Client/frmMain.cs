using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
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
                this.txtHttpRequest.AppendText(line + Environment.NewLine);
            };

            var now = DateTime.Now;
            this.txtCreatedate.Text = now.ToString("yyyy-MM-ddTHH:mm:ssZ");

            this.txtStartDate.Text = now.AddMonths(1).ToString("yyyy-MM-dd");
            this.txtEndDate.Text = now.AddMonths(1).AddDays(7).ToString("yyyy-MM-dd");

            this.txtBookingProperty1Checkin.Text = now.AddMonths(1).ToString("yyyy-MM-dd");
            this.txtBookingProperty1Checkout.Text = now.AddMonths(1).AddDays(7).ToString("yyyy-MM-dd");
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
                var client = CreateOAuth2Client();
                authState = client.GetClientAccessToken();
                UpdateFields();
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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-------------Error-------------");

            sb.AppendLine(ex.ToString());

            sb.AppendLine("");

            //if (ex.Status == WebExceptionStatus.ProtocolError)
            //{
            //    sb.AppendLine(string.Format("StatusCode:{0}", ex.Response.StatusCode));
            //    sb.AppendLine(string.Format("Status:{0}", ex.StatusDescription));
            //}
            //else
            //{
            using (WebResponse response = ex.Response)
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
            //}
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

        void LoadContent(string api)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(this.txtHost.Text + api);
                ClientBase.AuthorizeRequest(httpRequest, accessTokenTextBox.Text);



                var response = httpRequest.GetResponse();


                //  this.txtHttpRequest.Text = SharedTraceListener.Logs.ToString();

                StreamReader reader = new StreamReader(response.GetResponseStream());


                outputTextBox.Text = reader.ReadToEnd();



                //var requestReader = new StreamReader(httpRequest.GetRequestStream());
                //this.txtHttpRequest.Text = requestReader.ReadToEnd();


            }
            catch (WebException ex)
            {
                HandleWebException(ex);
                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine("-------------Error-------------");
                //sb.AppendLine(ex.ToString());

                //sb.AppendLine("");
                //using (WebResponse response = ex.Response)
                //{
                //    HttpWebResponse httpResponse = (HttpWebResponse)response;


                //    sb.AppendLine(string.Format("-------------Message-------------"));
                //    sb.AppendLine(string.Format("StatusCode:{0}", httpResponse.StatusCode));
                //    sb.AppendLine(string.Format("Status:{0}", httpResponse.StatusDescription));
                //    using (Stream data = response.GetResponseStream())
                //    {
                //        string text = new StreamReader(data).ReadToEnd();
                //        sb.AppendLine(text);
                //    }

                //    try
                //    {
                //        using (Stream data = response.GetResponseStream())
                //        {
                //            string text = new StreamReader(data).ReadToEnd();
                //            sb.AppendLine(text);
                //        }
                //    }
                //    catch
                //    {
                //    }
                //}
                //sb.AppendLine("");

                //outputTextBox.Text = sb.ToString();
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

        private void getResourceButton_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyApi.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertImagesApi.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyEnquiresApi.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyRatesApi.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyAvailabilityApi.Text);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertiesApi.Text);
        }

        private void btnPropertyAll_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyAllApi.Text);
        }

        private void btnBookingRules_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtBookingRules.Text);
        }

        private void btnBookingTags_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtBookingTags.Text);
        }

        private void btnPropertyExtras_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyExtras.Text);
        }

        private void btnGetSources_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtSources.Text);
        }

        private void btnContacts_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtContacts.Text);
        }

        private void btnPropertyRooms_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyRoomsApi.Text);
        }

        private void btnPropertyDistance_Click(object sender, EventArgs e)
        {
            this.LoadContent(this.txtPropertyDistanceApi.Text);
        }

        private void btnPostBooking_Click(object sender, EventArgs e)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("BookingTagIds", this.txtBookingTagIds.Text),
                new KeyValuePair<string, string>("EnquiryId", this.txtBookingEnquiryId.Text),
                new KeyValuePair<string, string>("Source", this.txtBookingSource.Text),

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
            this.PostContent(this.txtApiBooking.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private IEnumerable<KeyValuePair<string, string>> GetBookingProperties()
        {
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

        private void btnPostEnquiry_Click(object sender, EventArgs e)
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

            this.PostContent(this.txtApiImportEnquiry.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        void PostContent(string api, byte[] buffer)
        {
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(this.txtHost.Text + api);
                httpRequest.Method = "POST";

                //var buffer = System.Text.UTF8Encoding.UTF8.GetBytes(content);
                httpRequest.ContentLength = buffer.Length;
                httpRequest.ContentType = "application/x-www-form-urlencoded";
                ClientBase.AuthorizeRequest(httpRequest, accessTokenTextBox.Text);

                using (var postStream = httpRequest.GetRequestStream())
                {
                    postStream.Write(buffer, 0, buffer.Length);
                    postStream.Flush();
                    postStream.Close();
                }


                //var requestReader = new StreamReader(httpRequest.GetRequestStream());
                //this.txtHttpRequest.Text = requestReader.ReadToEnd();

                var response = httpRequest.GetResponse();

                StreamReader reader = new StreamReader(response.GetResponseStream());

                outputTextBox.Text = reader.ReadToEnd();

            }
            catch (WebException ex)
            {
                HandleWebException(ex);
                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine("-------------Error-------------");
                //sb.AppendLine(ex.ToString());

                //sb.AppendLine("");
                //using (WebResponse response = ex.Response)
                //{
                //    HttpWebResponse httpResponse = (HttpWebResponse)response;


                //    sb.AppendLine(string.Format("-------------Message-------------"));
                //    sb.AppendLine(string.Format("StatusCode:{0}", httpResponse.StatusCode));
                //    sb.AppendLine(string.Format("Status:{0}", httpResponse.StatusDescription));
                //    using (Stream data = response.GetResponseStream())
                //    {
                //        string text = new StreamReader(data).ReadToEnd();
                //        sb.AppendLine(text);
                //    }

                //    try
                //    {
                //        using (Stream data = response.GetResponseStream())
                //        {
                //            string text = new StreamReader(data).ReadToEnd();
                //            sb.AppendLine(text);
                //        }
                //    }
                //    catch
                //    {
                //    }
                //}
                //sb.AppendLine("");

                //outputTextBox.Text = sb.ToString();
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

        private void btnGetReviews_Click(object sender, EventArgs e)
        {
            this.LoadContent(txtReviewsApi.Text);
        }

        private void btnAddReview_Click(object sender, EventArgs e)
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

        private void btnPropertySearch_Click(object sender, EventArgs e)
        {
            LoadContent(txtPropertySearchApi.Text);
        }

        private void btnLocations_Click(object sender, EventArgs e)
        {
            LoadContent(txtLocations.Text);
        }

        private void btnDayAvailability_Click(object sender, EventArgs e)
        {
            LoadContent(txtDayAvailability.Text);
        }

        private void btnAmenities_Click(object sender, EventArgs e)
        {
            LoadContent(txtAmenities.Text);
        }

        private void btnAddPayment_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Amount", txtAmount.Text), 
                new KeyValuePair<string, string>("BookingId", txtBookingId.Text), 
                new KeyValuePair<string, string>("Charges", txtCharges.Text), 
                new KeyValuePair<string, string>("Currency", txtCurrency.Text), 
                new KeyValuePair<string, string>("Comments",txtComments.Text),
                new KeyValuePair<string, string>("PaymentCategoryId",txtPaymentCategoryId.Text),
                new KeyValuePair<string, string>("PaymentMethod",txtPaymentMethod.Text),
                new KeyValuePair<string, string>("PaymentTypeId",txtPaymentTypeId.Text),
                new KeyValuePair<string, string>("PaymentDate",txtPaymentDate.Text),
                new KeyValuePair<string, string>("Status",txtStatus.Text),
                new KeyValuePair<string, string>("PropertyId",txtPaymentPropertyId.Text)
            });

            PostContent(txtPaymentApi.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private void btnUpdatePropertyApi_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("liveWebsiteURL", txtLiveWebsiteURL.Text)
            });

            PostContent(txtUpdatePropertyApiUrl.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private void btnCalcBooking_Click(object sender, EventArgs e)
        {
            var formContent = new FormUrlEncodedContent(this.GetBookingCalcProperties());
            this.PostContent(this.txtApiBookingCalc.Text, formContent.ReadAsByteArrayAsync().Result);
        }

        private IEnumerable<KeyValuePair<string, string>> GetBookingCalcProperties()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Properties[0].Id", this.txtPropertyCalcPropertyId.Text),
                new KeyValuePair<string, string>("Properties[0].Checkin", this.txtPropertyCalcCheckIn.Text),
                new KeyValuePair<string, string>("Properties[0].Checkout", this.txtPropertyCalcCheckOut.Text),
                new KeyValuePair<string, string>("Properties[0].Adults", this.txtPropertyCalcAdults.Text),
                new KeyValuePair<string, string>("Properties[0].Children", this.txtPropertyCalcChildren.Text),
                new KeyValuePair<string, string>("Properties[0].Infants", this.txtPropertyCalcInfants.Text),
            };
        }
    }
}
