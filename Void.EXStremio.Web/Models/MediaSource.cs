namespace Void.EXStremio.Web.Models {
    class IMediaSource {
        public string ContentType { get; }

        public IMediaSource(string contentType) {
            ContentType = contentType;
        }
    }

    class StreamMediaSource : IMediaSource {
        public Stream Stream { get; }
        public long ContentLength { get; }

        public StreamMediaSource(string contentType, Stream stream, long contentLength) : base(contentType) {
            Stream = stream;
            ContentLength = contentLength;
        }
    }

    class PlaylistMediaSource : IMediaSource {
        public byte[] Content { get; }

        public PlaylistMediaSource(string contentType, byte[] content) : base(contentType) {
            Content = content;
        }
    }
}