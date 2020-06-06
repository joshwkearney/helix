#ifndef _REGIONS_H
#define _REGIONS_H

typedef struct $Region $Region;
typedef struct $RegionFrame $RegionFrame;

struct $RegionFrame {
	char* data_start;
	char* data_end;
	char* data_current;
	$RegionFrame* prev_frame;
};

struct $Region {
	$RegionFrame* frame;
};

static $RegionFrame* $make_region_frame() {
	int frame_size = 1024 * 1024;
	$RegionFrame* frame = malloc(sizeof($RegionFrame));

	frame->data_start = malloc(frame_size);
	frame->data_end = frame->data_start + frame_size;
	frame->data_current = frame->data_start;
	frame->prev_frame = NULL;

	return frame;
}

static void $free_region_frame($RegionFrame* frame) {
	while (frame != NULL) {
		free(frame->data_start);

		$RegionFrame* next = frame->prev_frame;
		free(frame);

		frame = next;
	}
}

static void* $region_malloc($Region* region, size_t size) {
	$RegionFrame* frame = region->frame;
	size_t frame_size = frame->data_end - frame->data_current;

	if (size > frame_size) {
		$RegionFrame* next = $make_region_frame();

		next->prev_frame = frame;
		region->frame = next;
	}

	void* result = frame->data_current;
	frame->data_current += size;

	return result;
}

#endif