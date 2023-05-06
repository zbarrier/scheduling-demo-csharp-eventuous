using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;

using Grpc.Core;

namespace DoctorDay.Domain.DayAggregate;

public static class DayExceptions
{
    public static DayException NewDayScheduleAlreadyArchived()
        => new DayException("The day has already been archived for the doctor.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);

    public static DayException NewDayScheduleAlreadyCancelled()
        => new DayException("The day has already been cancelled for the doctor.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);

    public static DayException NewDayAlreadyScheduled()
        => new DayException("The day is already scheduled for the doctor.", HttpStatusCode.Conflict, StatusCode.AlreadyExists);

    public static DayException NewDayNotScheduled()
        => new DayException("The day is not scheduled for the doctor.", HttpStatusCode.NotFound, StatusCode.NotFound);

    public static DayException NewDayAlreadyFull()
        => new DayException("The maximum number of slots have already been scheduled for the day.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);

    public static DayException NewSlotIsForWrongDay()
        => new DayException("The slot date is different than the date of the day you are trying to schedule it for.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);

    public static DayException NewSlotDurationInvalid()
        => new DayException("The slot duration is invalid.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);

    public static DayException NewSlotOverlaps()
        => new DayException("The slot overlaps with an already scheduled slot.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);

    public static DayException NewSlotNotScheduled()
        => new DayException("The slot is not scheduled.", HttpStatusCode.NotFound, StatusCode.NotFound);

    public static DayException NewSlotAlreadyBooked()
        => new DayException("The slot is already booked.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);

    public static DayException NewSlotNotBooked()
        => new DayException("The slot is not booked.", HttpStatusCode.BadRequest, StatusCode.FailedPrecondition);
}

[Serializable]
public sealed class DayException : Exception
{
    public DayException(int httpStatusCode, int grpcStatusCode) 
    {
        HttpStatusCode = httpStatusCode;
        GrpcStatusCode = grpcStatusCode;
    }
    public DayException(HttpStatusCode httpStatusCode, StatusCode grpcStatusCode)
    {
        HttpStatusCode = (int)httpStatusCode;
        GrpcStatusCode = (int)grpcStatusCode;
    }

    public DayException(string message, int httpStatusCode, int grpcStatusCode) 
        : base(message) 
    { 
        HttpStatusCode = httpStatusCode;
        GrpcStatusCode = grpcStatusCode;
    }
    public DayException(string message, HttpStatusCode httpStatusCode, StatusCode grpcStatusCode)
        : base(message)
    {
        HttpStatusCode = (int)httpStatusCode;
        GrpcStatusCode = (int)grpcStatusCode;
    }

    public DayException(string message, int httpStatusCode, int grpcStatusCode, Exception inner) 
        : base(message, inner) 
    {
        HttpStatusCode = httpStatusCode;
        GrpcStatusCode = grpcStatusCode;
    }
    public DayException(string message, HttpStatusCode httpStatusCode, StatusCode grpcStatusCode, Exception inner)
    : base(message, inner)
    {
        HttpStatusCode = (int)httpStatusCode;
        GrpcStatusCode = (int)grpcStatusCode;
    }

    DayException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        HttpStatusCode = info.GetInt32(nameof(HttpStatusCode));
        GrpcStatusCode = info.GetInt32(nameof(GrpcStatusCode));
    }

    public int HttpStatusCode { get; }
    public int GrpcStatusCode { get; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue(nameof(HttpStatusCode), HttpStatusCode);
        info.AddValue(nameof(GrpcStatusCode), GrpcStatusCode);
    }
}
