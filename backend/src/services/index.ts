/**
 * Services index
 * Exports all service modules for easy importing
 */

export { MetadataService } from './metadata.service';
export { ContentService } from './content.service';
export { LibraryService } from './library.service';
export { DownloadManager } from './download-manager.service';
export { NotificationService } from './notification.service';

// Export default instances
import metadataService from './metadata.service';
import contentService from './content.service';
import libraryService from './library.service';
import downloadManager from './download-manager.service';
import notificationService from './notification.service';

export default {
  metadataService,
  contentService,
  libraryService,
  downloadManager,
  notificationService
};
