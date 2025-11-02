using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RAG.AddressBook.Features.CreateContact;
using RAG.AddressBook.Features.DeleteContact;
using RAG.AddressBook.Features.GetContact;
using RAG.AddressBook.Features.GetProposal;
using RAG.AddressBook.Features.ImportContacts;
using RAG.AddressBook.Features.ListContacts;
using RAG.AddressBook.Features.ListProposals;
using RAG.AddressBook.Features.ProposeChange;
using RAG.AddressBook.Features.ReviewProposal;
using RAG.AddressBook.Features.SearchContacts;
using RAG.AddressBook.Features.UpdateContact;

namespace RAG.AddressBook.Endpoints;

public static class AddressBookEndpoints
{
    public static IEndpointRouteBuilder MapAddressBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/addressbook");

        // Contact management endpoints (Admin/PowerUser only)
        group.MapCreateContactEndpoint();
        group.MapGetContactEndpoint();
        group.MapListContactsEndpoint();
        group.MapUpdateContactEndpoint();
        group.MapDeleteContactEndpoint();
        group.MapSearchContactsEndpoint();
        group.MapImportContactsEndpoint();

        // Proposal management endpoints
        group.MapProposeChangeEndpoint();        // All authenticated users
        group.MapListProposalsEndpoint();        // Users see own, Admin sees all
        group.MapGetProposalEndpoint();          // Users see own, Admin sees all
        group.MapReviewProposalEndpoint();       // Admin/PowerUser only

        return app;
    }
}
