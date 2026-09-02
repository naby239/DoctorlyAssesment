# Plan

I need to build an API that has the first set of functions to be able to schedule appointments in a
doctors practice.

Here are the initial tasks I will create

## Creating the API

As a developer, I need to build out the API Skeleton that is going to be used to develop business
logic processing and feed back data to a client.

**Discussion**

The api need to have documentation so using swagger would make sense so that each endpoint is
documented. Keep in mind that some features might be internal.

Use CQRS with the mediator pattern as it suits the scheduling app

## Creating initial database entities

As a developer, I need to create entities that can be used to create these appointments

* Attendee Entity (Id, Name, Email address, Contact number, CreatedAt, Notification preference)
* Event Entity (Id, Title, Description, Start Time, End Time, Status, CreatedAt)
* Event Attendees (Id, EventId, AttendeeId, Attending, OptInNotify)

**Discussion**

Attendee will be saved so that they can be reused in future. The mapping table Event Attendees will
contain the attending an enum that contains the statuses.

## Event Create Function

As a developer, I need to implement a create event function in the API

* Controller Endpoint
* Business logic layer
* Data layer

## Event Update Function

As a developer, I need to implement a update event function in the API

* Controller Endpoint
* Business logic layer
* Data Layer

## Event Delete/Cancel Function

As a developer, I need to implement a delete/cancel event function in the API

* Controller endpoint
* Business logic layer
* Data layer

**Discussion**

Delete is not an option we would need to rather change the status on the event and then after a
period of time we could archive the events.

## Event Calendar list Function

As a developer, I need to implement an event calendar list function it should have the ability to
filter or search for an event.

* Controller endpoint
* Business logic layer
* Data layer

## Event Accept/Reject function

As a developer, I need to implement a event accept/reject function

* Controller endpoint
* Business logic layer
* Data layer

## Notification function

As a developer, I need to build out a notification capability.

* Email
* Whatsapp
* Push Notifications

**Discussion**

Notifications should be sent out when an event gets created, cancelled, updated. For push
notification we could use firebase cloud messaging

## Attendee availability

As a developer, I need to build out a feature where the attendees availability is checked before
creating an event with them included in.

* Create a check endpoint that returns information around the attendees in question
* Business logic to check the attendees availability
* Data layer
