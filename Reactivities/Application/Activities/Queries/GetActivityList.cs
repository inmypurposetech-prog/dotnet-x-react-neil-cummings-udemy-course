using System;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Activities.Queries;
// will contain the logic to get the list of activities from the DB
public class GetActivityList
{
    // mediatr queries are structured by specifying a class within a class
    // query will contain any query parameters as properties
    public class Query : IRequest<List<Activity>> {}

    // Because we are using this handler to retrieve data from our database we need to inject/import the AppDbContext into the constructor of the handler
    // Handles and returns the request with a the IRequest that we specified in the Query class
    public class Handler(AppDbContext context) : IRequestHandler<Query, List<Activity>>
    {
        public async Task<List<Activity>> Handle(Query request, CancellationToken cancellationToken)
        {
            return await context.Activities.ToListAsync(cancellationToken);
        }
    }
}
