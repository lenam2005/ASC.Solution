using ASC.Model.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ASC.Utilities;

namespace ASC.Model.Queries
{
    public static class Queries
    {
        public static Expression<Func<ServiceRequest, bool>> GetDashboardQuery(
            DateTime? requestedDate,
            List<string>? status = null,
            string email = "",
            string serviceEngineerEmail = "")
        {
            var query = (Expression<Func<ServiceRequest, bool>>)(u => true);

            if (requestedDate.HasValue)
            {
                var requestedDateFilter =
                    (Expression<Func<ServiceRequest, bool>>)(u => u.RequestedDate >= requestedDate.Value);

                query = query.And(requestedDateFilter);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();

                var emailFilter =
                    (Expression<Func<ServiceRequest, bool>>)(u => u.PartitionKey.StartsWith(email));

                query = query.And(emailFilter);
            }

            if (!string.IsNullOrWhiteSpace(serviceEngineerEmail))
            {
                serviceEngineerEmail = serviceEngineerEmail.Trim();

                var serviceEngineerFilter =
                    (Expression<Func<ServiceRequest, bool>>)(u => u.ServiceEngineer == serviceEngineerEmail);

                query = query.And(serviceEngineerFilter);
            }

            var statusQueries = (Expression<Func<ServiceRequest, bool>>)(u => false);

            if (status != null && status.Count > 0)
            {
                foreach (var state in status)
                {
                    var currentState = state.Trim();

                    var statusFilter =
                        (Expression<Func<ServiceRequest, bool>>)(u => u.Status == currentState);

                    statusQueries = statusQueries.Or(statusFilter);
                }

                query = query.And(statusQueries);
            }

            return query;
        }
    }
}