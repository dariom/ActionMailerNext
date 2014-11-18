﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActionMailerNext.Interfaces;

namespace ActionMailerNext
{
    /// <summary>
    ///     Some helpers used to deliver mail.  Reduces the need to repeat code.
    /// </summary>
    public class DeliveryHelper
    {
        private readonly IMailInterceptor _interceptor;
        private readonly IMailSender _sender;

        /// <summary>
        ///     Creates a new delivery helper to be used for sending messages.
        /// </summary>
        /// <param name="sender">The sender to use when delivering mail.</param>
        /// <param name="interceptor">The interceptor to report with while delivering mail.</param>
        public DeliveryHelper(IMailSender sender, IMailInterceptor interceptor)
        {
            if (interceptor == null)
                throw new ArgumentNullException("interceptor");

            if (sender == null)
                throw new ArgumentNullException("sender");

            _sender = sender;
            _interceptor = interceptor;
        }

        /// <summary>
        ///     Sends the given email
        /// </summary>
        /// <param name="mail">The mail message to send.</param>
        public MailAttributes Deliver(MailAttributes mail)
        {
            if (mail == null)
                throw new ArgumentNullException("mail");

            var mailContext = new MailSendingContext(mail);
            _interceptor.OnMailSending(mailContext);

            if (mailContext.Cancel)
                return null;

            _sender.Send(mail);
            _interceptor.OnMailSent(mail);

            return mail;
        }

        /// <summary>
        ///     Sends async the given email
        /// </summary>
        /// <param name="mail">The mail message to send.</param>
        public async Task<MailAttributes> DeliverAsync(MailAttributes mail)
        {
            if (mail == null)
                throw new ArgumentNullException("mail");

            var mailContext = new MailSendingContext(mail);
            _interceptor.OnMailSending(mailContext);

            if (mailContext.Cancel)
                return null;

            var sendtask = _sender.SendAsync(mail);
            await sendtask.ContinueWith(t => AsyncSendCompleted(mail));
            return mail;
        }

        private void AsyncSendCompleted(MailAttributes mail)
        {
            _interceptor.OnMailSent(mail);
        }
    }
}