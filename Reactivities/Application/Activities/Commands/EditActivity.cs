using System;
using System.Runtime.InteropServices;
using AutoMapper;
using Domain;
using MediatR;
using Persistence;

namespace Application.Activities.Commands;

public class EditActivity
{
    public class Command : IRequest
    {
        public required Activity Activity { get; set; }
    }

    public class Handler(AppDbContext context, IMapper mapper) : IRequestHandler<Command>
    {
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            var activity = await context.Activities.FindAsync([request.Activity.Id], cancellationToken ) 
                ?? throw new Exception("Activity not Found");

            // This is a possible approach for mapping the data but we are going to be using automapper
            // activity.Title = request.Activity.Title;
            // activity.Description = request.Activity.Description;
            // activity.Date = request.Activity.Date;
            // activity.Category = request.Activity.Category;

            // automapper - nuget package to install
            // automapper implementation ---> requires additional configuration which is handled in Application > Core > MappingProfiles
            mapper.Map(request.Activity, activity);
            await context.SaveChangesAsync(cancellationToken);

            // Cancellation tokens are theoretically a way to cancel ongoing operations
            // e.g where an operation is taking longer than expected to complete and a user 
            // navigates away from a page before the operation completes then a cancellation token would be used to cancel the ongoing operation
        }
    }
}
