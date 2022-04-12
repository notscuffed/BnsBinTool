#include <cinttypes>

#include <vector>

#include <isa-l.h>

#define ISALIB_EXTERN extern "C" __declspec(dllexport)

using byte = unsigned char;

ISALIB_EXTERN bool zlib_decompress(byte* compressed_data, size_t compressed_size, byte* buffer, size_t buffer_size)
{
  inflate_state stream;
  isal_inflate_init(&stream);
  stream.hist_bits = ISAL_DEF_MAX_HIST_BITS;
  stream.crc_flag = ISAL_ZLIB;
  stream.next_in = compressed_data;
  stream.avail_in = compressed_size;
  stream.next_out = buffer;
  stream.avail_out = buffer_size;

  size_t err = isal_inflate(&stream);

  return err == ISAL_DECOMP_OK;
}

ISALIB_EXTERN size_t zlib_compress(byte* data, size_t data_size, byte* buffer, size_t buffer_size, byte* level_buffer)
{
  isal_zstream stream;
  isal_deflate_init(&stream);
  stream.hist_bits = ISAL_DEF_MAX_HIST_BITS;
  stream.gzip_flag = ISAL_ZLIB;
  stream.level = 2;
  stream.level_buf = level_buffer;
  stream.level_buf_size = ISAL_DEF_LVL2_DEFAULT;
  stream.next_in = data;
  stream.avail_in = data_size;
  stream.next_out = buffer;
  stream.avail_out = buffer_size;

  size_t err = isal_deflate_stateless(&stream);

  if (err != COMP_OK)
    return 0;

  return stream.total_out;
}
