﻿namespace ActionMailer.Net.Mvc3 {
    using System;
    using System.IO;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Text;
    using System.Web.Mvc;

    /// <summary>
    /// A special result that should be returned from each eaction in your
    /// mail controller.  Your controller must inherit from MailerBase.
    /// </summary>
    public class EmailResult : ViewResult, IEmailResult {
        private readonly IMailInterceptor _interceptor;
        private readonly DeliveryHelper _deliveryHelper;
        private readonly bool _trimBody;

        private IView _htmlView;
        private string _htmlViewName;

        private IView _textView;
        private string _textViewName;

        /// <summary>
        /// The underlying MailMessage object that was passed to this object's constructor.
        /// </summary>
        public MailMessage Mail { get { return this._mail; } }
        private readonly MailMessage _mail;

        /// <summary>
        /// The IMailSender instance that is used to deliver mail.
        /// </summary>
        public IMailSender MailSender { get { return this._mailSender; } }
        private readonly IMailSender _mailSender;

        /// <summary>
        /// The default encoding used to send a message.
        /// </summary>
        public Encoding MessageEncoding { get { return this._messageEncoding; } }
        private readonly Encoding _messageEncoding;

        /// <summary>
        /// Creates a new EmailResult.  You must call ExecuteCore() before this result
        /// can be successfully delivered.
        /// </summary>
        /// <param name="interceptor">The IMailInterceptor that we will call when delivering mail.</param>
        /// <param name="sender">The IMailSender that we will use to send mail.</param>
        /// <param name="mail">The mail message who's body needs populating.</param>
        /// <param name="viewName">The view to use when rendering the message body (can be null)</param>
        /// <param name="masterName">The maste rpage to use when rendering the message body (can be null)</param>
        /// <param name="messageEncoding">The encoding to use when rendering a message.</param>
        /// <param name="trimBody">Whether or not we should trim whitespace from the beginning and end of the message body.</param>
        public EmailResult(IMailInterceptor interceptor, IMailSender sender, MailMessage mail, string viewName, string masterName, Encoding messageEncoding, bool trimBody) {
            if (interceptor == null)
                throw new ArgumentNullException("interceptor");

            if (sender == null)
                throw new ArgumentNullException("sender");

            if (mail == null)
                throw new ArgumentNullException("mail");

            this.ViewName = viewName ?? this.ViewName;
            this.MasterName = masterName ?? this.MasterName;
            this._messageEncoding = messageEncoding;
            this._mail = mail;
            this._mailSender = sender;
            this._interceptor = interceptor;
            this._deliveryHelper = new DeliveryHelper(sender, interceptor);
            this._trimBody = trimBody;
        }

        /// <summary>
        /// Causes the body of the mail message to be generated.
        /// </summary>
        /// <param name="context">The controller context to use while rendering the body.</param>
        public override void ExecuteResult(ControllerContext context) {
            this.AddMessageViews(context);
        }

        /// <summary>
        /// Sends your message.  This call will block while the message is being sent. (not recommended)
        /// </summary>
        public void Deliver() {
            this._deliveryHelper.Deliver(false, this.Mail);
        }

        /// <summary>
        /// Sends your message asynchronously.  This method does not block.  If you need to know
        /// when the message has been sent, then override the OnMailSent method in MailerBase which
        /// will not fire until the asyonchronous send operation is complete.
        /// </summary>
        public void DeliverAsync() {
            this._deliveryHelper.Deliver(true, this.Mail);
        }

        private void LocateViews(ControllerContext context) {
            if (context == null)
                throw new ArgumentNullException("context");

            if (string.IsNullOrEmpty(this.ViewName))
                this.ViewName = context.RouteData.GetRequiredString("action");

            this._htmlViewName = String.Format("{0}.html", this.ViewName);
            this._textViewName = String.Format("{0}.txt", this.ViewName);

            var htmlViewResult = ViewEngines.Engines.FindView(context, this._htmlViewName, this.MasterName);
            if (htmlViewResult.View != null) {
                this._htmlView = htmlViewResult.View;
            }

            var textViewResult = ViewEngines.Engines.FindView(context, this._textViewName, this.MasterName);
            if (textViewResult.View != null) {
                this._textView = textViewResult.View;
            }
        }

        private string RenderViewAsString(ControllerContext context, IView view) {
            using (var writer = new StringWriter()) {
                var viewContext = new ViewContext(context, view, this.ViewData, this.TempData, writer);
                view.Render(viewContext, writer);

                string output = writer.GetStringBuilder().ToString();
                if (this._trimBody)
                    output = output.Trim();

                return output;
            }
        }

        private void AddMessageViews(ControllerContext context) {
            if (context == null)
                throw new ArgumentNullException("context");

            this.LocateViews(context);

            if (this._textView == null && this._htmlView == null) {
                var message = String.Format("You must provide a view for this email.  Views should be named {0}.txt.cshtml or {1}.html.cshtml (or aspx for WebFormsViewEngine) depending on the format you wish to render.", this.ViewName, this.ViewName);
                throw new NoViewsFoundException(message);
            }

            if (this._textView != null) {
                var body = this.RenderViewAsString(context, this._textView);
                var altView = AlternateView.CreateAlternateViewFromString(body, this.MessageEncoding ?? Encoding.Default, MediaTypeNames.Text.Plain);
                this.Mail.AlternateViews.Add(altView);
            }

            if (this._htmlView != null) {
                var body = this.RenderViewAsString(context, this._htmlView);
                var altView = AlternateView.CreateAlternateViewFromString(body, this.MessageEncoding ?? Encoding.Default, MediaTypeNames.Text.Html);
                this.Mail.AlternateViews.Add(altView);
            }
        }
    }
}