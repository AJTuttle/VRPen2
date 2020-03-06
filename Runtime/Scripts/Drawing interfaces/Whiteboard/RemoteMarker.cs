using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {
	public class RemoteMarker : InputVisuals {

		public Transform snappedTo;
		protected float raycastDistance = 0.2f;
		public Transform modelParent;
		bool snappedToChecker = false;


        private void Update() {
			if (snappedTo) {
				Vector3 origin = transform.position - snappedTo.forward * raycastDistance;
				RaycastHit[] hits = Physics.RaycastAll(origin, snappedTo.forward, 1f);
				
				//move marker pos;
				foreach(RaycastHit hit in hits) {
					
					Tag tag = hit.collider.GetComponent<Tag>();

					if (tag != null && tag.tag.Equals("Draw_Area")) {

						modelParent.position = hit.point;
						break;

					}

				}

			}

		}

		private void OnTriggerEnter(Collider other) {
			Tag tag;
			if ((tag = other.gameObject.GetComponent<Tag>()) != null) {
				//if (tag.tag.Equals("marker-visible")) visuals.SetActive(true);
				if (tag.tag.Equals("snap")) {
					snappedTo = other.transform;
					snappedToChecker = true;

				}
			}
		}

		private void OnTriggerStay(Collider other) {
			if (snappedTo != null && other.transform == snappedTo) snappedToChecker = true;
		}

		private void OnTriggerExit(Collider other) {

			Tag tag;
			if ((tag = other.gameObject.GetComponent<Tag>()) != null) {
				//if (tag.tag.Equals("marker-visible")) visuals.SetActive(false);
				if (tag.tag.Equals("snap") && snappedTo != null) {
					
					snappedTo = null;
					modelParent.localPosition = Vector3.zero;

				}

			}

		}

		private void FixedUpdate() {
			if (snappedToChecker) {
				snappedToChecker = false;
			}
			else {
				if (snappedTo != null) {
					snappedTo = null;
					modelParent.localPosition = Vector3.zero;
				}
			}
		}
	}
}