namespace DomainFixture.SourceGenerator.Models;

internal enum DomainOperationManifestFailureKind
{
    UnsupportedOperationSchema,
    MalformedOperationManifest,
    UnsupportedOperationKind,
    InvalidOperationIdentity,
    CallableNotFound,
    CallableAmbiguous,
    SignatureMismatch,
    ReturnTypeMismatch,
    InvalidParameterBinding,
    ConflictingOperation,
    UnsupportedOutcomeSchema,
    MalformedOutcomeManifest,
    InvalidOutcome,
    ConflictingOutcome,
    OrphanOutcome
}
